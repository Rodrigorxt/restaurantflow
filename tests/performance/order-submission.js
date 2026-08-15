import http from 'k6/http';
import { check, fail } from 'k6';
import { Counter, Rate, Trend } from 'k6/metrics';

const baseUrl = __ENV.BASE_URL || 'http://localhost:8080';
const identityUrl = __ENV.IDENTITY_URL || 'http://localhost:8081';

const orderSubmissionDuration = new Trend('order_submission_duration', true);
const orderSubmissionFailures = new Rate('order_submission_failures');
const acceptedOrders = new Counter('accepted_orders');

export const options = {
  scenarios: {
    order_submission: {
      executor: 'constant-arrival-rate',
      rate: Number(__ENV.RATE || 5),
      timeUnit: '1s',
      duration: __ENV.DURATION || '30s',
      preAllocatedVUs: Number(__ENV.PRE_ALLOCATED_VUS || 10),
      maxVUs: Number(__ENV.MAX_VUS || 30),
      gracefulStop: '10s',
    },
  },
  thresholds: {
    checks: ['rate>0.99'],
    http_req_failed: ['rate<0.01'],
    order_submission_failures: ['rate<0.01'],
    order_submission_duration: ['p(95)<1500', 'p(99)<2500'],
    dropped_iterations: ['count==0'],
  },
};

function tokenFor(username, password) {
  const response = http.post(
    `${identityUrl}/realms/restaurantflow/protocol/openid-connect/token`,
    {
      grant_type: 'password',
      client_id: 'restaurantflow-cli',
      username,
      password,
    },
    { tags: { name: 'Acquire token' } },
  );

  if (response.status !== 200) {
    fail(`Token request failed for ${username}: ${response.status}`);
  }

  return response.json('access_token');
}

export function setup() {
  const adminToken = tokenFor('restaurant-admin', 'admin');
  const customerToken = tokenFor('customer', 'customer');
  const suffix = Date.now();
  const menuResponse = http.post(
    `${baseUrl}/api/menu/items`,
    JSON.stringify({
      name: `Performance Burger ${suffix}`,
      description: 'Server-priced k6 workload item',
      category: 'Main',
      price: 19.95,
    }),
    {
      headers: {
        Authorization: `Bearer ${adminToken}`,
        'Content-Type': 'application/json',
      },
      tags: { name: 'Create workload menu item' },
    },
  );

  if (menuResponse.status !== 201) {
    fail(`Menu setup failed: ${menuResponse.status} ${menuResponse.body}`);
  }

  return {
    customerToken,
    menuItemId: menuResponse.json('id'),
    runId: String(suffix),
  };
}

export default function (data) {
  const response = http.post(
    `${baseUrl}/api/orders`,
    JSON.stringify({
      customerId: '00000000-0000-0000-0000-000000000000',
      customerEmail: 'ignored@example.com',
      paymentReference: `load-${data.runId}-${__VU}-${__ITER}`,
      items: [{ menuItemId: data.menuItemId, quantity: 1 }],
    }),
    {
      headers: {
        Authorization: `Bearer ${data.customerToken}`,
        'Content-Type': 'application/json',
      },
      tags: { name: 'Submit order' },
    },
  );

  const accepted = check(response, {
    'order accepted': (result) => result.status === 202,
    'order id returned': (result) => Boolean(result.json('id')),
    'server total returned': (result) => result.json('total') === 19.95,
  });

  orderSubmissionDuration.add(response.timings.duration);
  orderSubmissionFailures.add(!accepted);
  if (accepted) acceptedOrders.add(1);
}

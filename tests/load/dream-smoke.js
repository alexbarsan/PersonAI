import http from "k6/http";
import { check, sleep } from "k6";

const baseUrl = __ENV.DREAMLENS_BASE_URL || "http://localhost:5000";
const token = __ENV.DREAMLENS_TEST_TOKEN || "";

export const options = {
  thresholds: {
    http_req_failed: ["rate<0.05"],
    http_req_duration: ["p(95)<3000"],
  },
  scenarios: {
    smoke: {
      executor: "constant-vus",
      vus: 1,
      duration: "30s",
    },
  },
};

export default function () {
  const live = http.get(`${baseUrl}/health/live`);
  check(live, {
    "live health is ok": (response) => response.status === 200,
  });

  const ready = http.get(`${baseUrl}/health/ready`);
  check(ready, {
    "ready health is ok or unavailable": (response) => response.status === 200 || response.status === 503,
  });

  if (token) {
    const dream = http.post(
      `${baseUrl}/v1/dreams`,
      JSON.stringify({
        text: "I was walking through a bright hallway and looking for a door that felt familiar.",
        mood: "curious",
        sleepQuality: 4,
        tags: ["smoke"],
        occurredAt: "2026-07-02",
      }),
      {
        headers: {
          Authorization: `Bearer ${token}`,
          "Content-Type": "application/json",
        },
      },
    );

    check(dream, {
      "dream endpoint is reachable": (response) => [200, 400, 401, 429, 503].includes(response.status),
    });
  }

  sleep(1);
}

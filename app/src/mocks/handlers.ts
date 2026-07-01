import { rest } from "msw";

import { mockDream, mockInsights, mockJournal, mockMe, mockProfile } from "@/mocks/mockData";

export const handlers = [
  rest.get("http://localhost/v1/me", (_, response, context) => response(context.json(mockMe))),
  rest.get("http://localhost/v1/profile", (_, response, context) => response(context.json(mockProfile))),
  rest.post("http://localhost/v1/dreams", (_, response, context) => response(context.json(mockDream))),
  rest.get("http://localhost/v1/dreams", (_, response, context) => response(context.json(mockJournal))),
  rest.get("http://localhost/v1/dreams/:id", (_, response, context) => response(context.json(mockDream))),
  rest.get("http://localhost/v1/insights", (_, response, context) => response(context.json(mockInsights)))
];

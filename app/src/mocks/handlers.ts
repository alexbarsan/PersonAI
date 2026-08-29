import { rest } from "msw";

import { mockDream, mockDreamImage, mockEntitlement, mockInsights, mockJournal, mockMe, mockProfile } from "@/mocks/mockData";

export const handlers = [
  rest.get("http://localhost/v1/me", (_, response, context) => response(context.json(mockMe))),
  rest.get("http://localhost/v1/profile", (_, response, context) => response(context.json(mockProfile))),
  rest.put("http://localhost/v1/profile", async (request, response, context) =>
    response(context.json(await request.json()))),
  rest.post("http://localhost/v1/dreams", (_, response, context) => response(context.json(mockDream))),
  rest.get("http://localhost/v1/dreams", (_, response, context) => response(context.json(mockJournal))),
  rest.post("http://localhost/v1/dreams/:id/image", (_, response, context) => response(context.status(202), context.json(mockDreamImage))),
  rest.get("http://localhost/v1/dreams/:id/image", (_, response, context) => response(context.json(mockDreamImage))),
  rest.get("http://localhost/v1/dreams/:id", (_, response, context) => response(context.json(mockDream))),
  rest.delete("http://localhost/v1/dreams/:id", (_, response, context) => response(context.status(204))),
  rest.get("http://localhost/v1/insights", (_, response, context) => response(context.json(mockInsights))),
  rest.get("http://localhost/v1/entitlements", (_, response, context) => response(context.json(mockEntitlement)))
];

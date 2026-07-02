namespace DreamLens.Api.Infrastructure.OpenApi;

public static class SwaggerUiEndpoints
{
    public static IEndpointRouteBuilder MapDreamLensSwaggerUi(this IEndpointRouteBuilder app)
    {
        app.MapGet("/swagger", (HttpContext context) =>
            {
                context.Response.Headers.ContentSecurityPolicy =
                    "default-src 'none'; " +
                    "style-src 'self' 'unsafe-inline' https://unpkg.com; " +
                    "script-src 'self' 'unsafe-inline' https://unpkg.com; " +
                    "img-src 'self' data: https://validator.swagger.io; " +
                    "connect-src 'self'; " +
                    "frame-ancestors 'none'; " +
                    "base-uri 'none'";

                return Results.Content(SwaggerHtml, "text/html");
            })
            .WithName("GetSwaggerUi")
            .WithSummary("Serves the development Swagger UI.");

        return app;
    }

    private const string SwaggerHtml = """
        <!doctype html>
        <html lang="en">
        <head>
          <meta charset="utf-8">
          <meta name="viewport" content="width=device-width, initial-scale=1">
          <title>DreamLens API Swagger</title>
          <link rel="stylesheet" href="https://unpkg.com/swagger-ui-dist@5/swagger-ui.css">
          <style>
            body { margin: 0; background: #f8faf9; }
            .topbar { display: none; }
          </style>
        </head>
        <body>
          <div id="swagger-ui"></div>
          <script src="https://unpkg.com/swagger-ui-dist@5/swagger-ui-bundle.js"></script>
          <script>
            window.ui = SwaggerUIBundle({
              url: "/openapi/v1.json",
              dom_id: "#swagger-ui",
              deepLinking: true,
              presets: [SwaggerUIBundle.presets.apis],
              layout: "BaseLayout"
            });
          </script>
        </body>
        </html>
        """;
}

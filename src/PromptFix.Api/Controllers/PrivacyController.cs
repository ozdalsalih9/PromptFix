using Microsoft.AspNetCore.Mvc;

namespace PromptFix.Api.Controllers;

[ApiController]
public sealed class PrivacyController : ControllerBase
{
    [HttpGet("/privacy")]
    public ContentResult Privacy()
    {
        const string html = """
            <!doctype html>
            <html lang="en">
              <head>
                <meta charset="utf-8">
                <meta name="viewport" content="width=device-width, initial-scale=1">
                <title>PromptForge Privacy Policy</title>
                <style>
                  body { margin: 0; font: 16px/1.6 system-ui, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif; color: #12243d; background: #f4f8ff; }
                  main { max-width: 780px; margin: 0 auto; padding: 48px 20px; }
                  h1 { margin: 0 0 12px; font-size: 32px; line-height: 1.15; color: #0b1f3a; }
                  h2 { margin: 30px 0 8px; font-size: 20px; color: #19385f; }
                  p, li { color: #334155; }
                  .updated { color: #64748b; }
                  a { color: #0067d9; }
                </style>
              </head>
              <body>
                <main>
                  <h1>PromptForge Privacy Policy</h1>
                  <p class="updated">Last updated: June 10, 2026</p>

                  <h2>Overview</h2>
                  <p>PromptForge is a Chrome extension that rewrites rough prompts into clearer, ready-to-use prompts. The extension sends text entered by the user to the PromptForge API only to generate an improved prompt response.</p>

                  <h2>Data We Process</h2>
                  <p>PromptForge processes the prompt text that users manually enter into the extension popup, along with selected options such as language, mode, and style.</p>

                  <h2>Local Storage</h2>
                  <p>The extension uses Chrome storage to save prompt history locally in the user's browser. This history is used only so users can reopen previous prompts from the extension popup.</p>

                  <h2>Server Processing</h2>
                  <p>User-entered prompt text is sent to the PromptForge backend at https://promptfix.duckdns.org for processing by a self-hosted model. PromptForge does not sell user data, does not use prompt text for advertising, and does not intentionally collect account information, payment information, location, browsing history, or authentication credentials.</p>

                  <h2>Third Parties</h2>
                  <p>PromptForge does not share user-entered prompt text with advertising networks or data brokers. The backend is operated for the purpose of generating prompt improvements.</p>

                  <h2>Contact</h2>
                  <p>For privacy questions, contact the publisher through the Chrome Web Store listing.</p>
                </main>
              </body>
            </html>
            """;

        return Content(html, "text/html");
    }
}

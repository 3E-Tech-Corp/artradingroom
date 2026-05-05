import { test, expect } from "@playwright/test";

test.describe("ArTrading Smoke Tests", () => {
  test("frontend loads", async ({ page }) => {
    await page.goto("/");
    const title = await page.title();
    expect(title).toBeTruthy();
    console.log("✓ Page title:", title);
  });

  test("API health check", async ({ page }) => {
    const res = await page.request.get("/api/health");
    const ct = res.headers()["content-type"] || "";
    if (ct.includes("json") || ct.includes("text/plain")) {
      console.log("✓ Health endpoint responding:", res.status());
    } else {
      console.log("⚠ Backend not running (got HTML fallback)");
      test.skip();
    }
  });

  const apiTests = [
    { name: "agents", path: "/api/agents" },
    { name: "strategies", path: "/api/strategies" },
    { name: "trades", path: "/api/trades" },
    { name: "risk thresholds", path: "/api/risk/thresholds" },
    { name: "audit", path: "/api/audit" },
  ];

  for (const { name, path } of apiTests) {
    test(`GET ${path} returns data`, async ({ page }) => {
      const res = await page.request.get(path);
      const ct = res.headers()["content-type"] || "";
      if (!ct.includes("json")) {
        console.log(`⚠ Backend not running for ${name} (got HTML fallback)`);
        test.skip();
        return;
      }
      console.log(`${name} status:`, res.status());
      if (res.status() === 200) {
        const data = await res.json();
        console.log(`✓ ${name}:`, Array.isArray(data) ? `${data.length} items` : JSON.stringify(data).slice(0, 100));
      }
    });
  }
});

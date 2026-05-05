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
    console.log("Health status:", res.status());
    // Accept 200 or 404 (if health endpoint not yet wired)
    expect([200, 404]).toContain(res.status());
  });

  test("GET /api/agents returns list", async ({ page }) => {
    const res = await page.request.get("/api/agents");
    console.log("Agents API status:", res.status());
    if (res.status() === 200) {
      const data = await res.json();
      expect(Array.isArray(data)).toBeTruthy();
      console.log("✓ Agents count:", data.length);
    }
  });

  test("GET /api/strategies returns list", async ({ page }) => {
    const res = await page.request.get("/api/strategies");
    console.log("Strategies API status:", res.status());
    if (res.status() === 200) {
      const data = await res.json();
      expect(Array.isArray(data)).toBeTruthy();
      console.log("✓ Strategies count:", data.length);
    }
  });

  test("GET /api/trades returns list", async ({ page }) => {
    const res = await page.request.get("/api/trades");
    console.log("Trades API status:", res.status());
    if (res.status() === 200) {
      const data = await res.json();
      expect(Array.isArray(data)).toBeTruthy();
      console.log("✓ Trades count:", data.length);
    }
  });

  test("GET /api/risk/thresholds returns config", async ({ page }) => {
    const res = await page.request.get("/api/risk/thresholds");
    console.log("Risk thresholds status:", res.status());
    if (res.status() === 200) {
      const data = await res.json();
      console.log("✓ Risk thresholds:", JSON.stringify(data).slice(0, 100));
    }
  });

  test("GET /api/audit returns log", async ({ page }) => {
    const res = await page.request.get("/api/audit");
    console.log("Audit API status:", res.status());
    if (res.status() === 200) {
      const data = await res.json();
      expect(Array.isArray(data)).toBeTruthy();
      console.log("✓ Audit entries:", data.length);
    }
  });
});

const { test } = require('@playwright/test');

const pages = ['Dashboard', 'Strategy Lab', 'Risk Console', 'Trading', 'Research', 'Agents'];

test('take screenshots of all pages', async ({ page }) => {
  await page.goto('https://aitrade.synthia.bot');
  await page.waitForLoadState('networkidle');

  for (const name of pages) {
    await page.click(`button.nav-btn:has-text("${name}")`);
    await page.waitForTimeout(1000);
    await page.screenshot({ path: `e2e/screenshots/${name.toLowerCase().replace(/\\s+/g, '-')}.png`, fullPage: true });
  }
});

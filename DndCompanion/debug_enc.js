const { chromium } = require('playwright');
(async () => {
  const browser = await chromium.launch({ channel: 'chrome' });
  const page = await browser.newPage();
  await page.goto('http://127.0.0.1:5299/campaigns/6909352D-D033-41E3-BA4B-B3DE7C4D470C');
  await page.waitForSelector('.section-card__title:has-text("Encounters")');
  const btn = page.locator('button:has-text("+ New Encounter")');
  console.log('disabled?', await btn.isDisabled());
  await btn.click();
  await page.waitForTimeout(500);
  const html = await page.locator('.section-card:has-text("Encounters")').innerHTML();
  console.log(html.slice(0, 2000));
  await browser.close();
})();

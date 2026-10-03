import { formatDate } from '@angular/common';
import { INDIA_DATE_PIPE_TIMEZONE, INDIA_TIME_ZONE } from './application-time';

describe('application time zone', () => {
  it('formats UTC instants in India Standard Time', () => {
    const value = new Date('2026-01-01T00:00:00Z');

    expect(INDIA_TIME_ZONE).toBe('Asia/Kolkata');
    expect(formatDate(value, 'yyyy-MM-dd HH:mm', 'en-IN', INDIA_DATE_PIPE_TIMEZONE))
      .toBe('2026-01-01 05:30');
  });
});

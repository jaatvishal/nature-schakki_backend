export const ORDER_STATUS_STEPS = [
  'Pending',
  'PaymentReceived',
  'Processing',
  'Packed',
  'Shipped',
  'OutForDelivery',
  'Delivered',
] as const;

export const COD_ORDER_STATUS_STEPS = [
  'Pending',
  'Processing',
  'Packed',
  'Shipped',
  'OutForDelivery',
  'Delivered',
] as const;

export const ADMIN_ORDER_STATUSES = [
  ...ORDER_STATUS_STEPS,
  'Cancelled',
  'Refunded',
  'Failed',
] as const;

export function orderStatusLabel(status: string): string {
  if (status === 'PaymentReceived') return 'Paid';
  return status.replace(/([A-Z])/g, ' $1').trim();
}

export function orderStepIndex(status: string, steps: readonly string[] = ORDER_STATUS_STEPS): number {
  const idx = steps.indexOf(status);
  if (idx >= 0) return idx;
  if (status === 'Cancelled' || status === 'Refunded' || status === 'Failed') return -1;
  return 0;
}

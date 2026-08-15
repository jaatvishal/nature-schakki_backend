export type HeaderItemId = 'logo' | 'nav' | 'search' | 'cart' | 'user-menu';

export type HeaderItemConfig = {
  id: HeaderItemId;
  enabled: boolean;
};

export const HEADER_CONFIG: HeaderItemConfig[] = [
  { id: 'logo', enabled: true },
  { id: 'nav', enabled: true },
  { id: 'search', enabled: true },
  { id: 'cart', enabled: true },
  { id: 'user-menu', enabled: true },
];

export function getEnabledHeaderItems(): HeaderItemConfig[] {
  return HEADER_CONFIG.filter(item => item.enabled);
}

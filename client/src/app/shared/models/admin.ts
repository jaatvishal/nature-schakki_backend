import { OrderItem } from './order';

export type PagedResult<T> = {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
};

export type AdminUser = {
  id: number;
  name: string;
  email: string;
  emailVerified: boolean;
  isActive: boolean;
  createdAt: string;
  lastLoginAt?: string;
  totalOrders: number;
  totalSpent: number;
};

export type AdminProduct = {
  id: number;
  name: string;
  description: string;
  sku: string;
  brand: string;
  category: string;
  price: number;
  stock: number;
  unit: string;
  pictureUrl: string;
  isActive: boolean;
  isArchived: boolean;
};

export type AdminProductInput = Omit<AdminProduct, 'id' | 'isArchived'>;

export type AdminCategory = {
  id: number;
  name: string;
  description?: string;
  isActive: boolean;
  productCount: number;
};

export type AdminOrder = {
  id: number;
  userId: number;
  customer: string;
  orderDate: string;
  status: string;
  paymentMethod: string;
  paymentStatus: string;
  subtotal: number;
  deliveryCost: number;
  discount: number;
  total: number;
  orderItems: OrderItem[];
  timeline: {
    fromStatus: string;
    toStatus: string;
    changedByUserId: number;
    changedAt: string;
  }[];
};

export type AdminInventory = {
  productId: number;
  productName: string;
  sku: string;
  quantityOnHand: number;
  reservedQuantity: number;
  availableQuantity: number;
  reorderLevel: number;
  isLowStock: boolean;
};

export type InventoryMovement = {
  id: number;
  productId: number;
  quantityChange: number;
  previousQuantity: number;
  newQuantity: number;
  reason: string;
  actorUserId?: number;
  orderId?: number;
  createdAt: string;
};

export type AdminPayment = {
  id?: number;
  orderId: number;
  customer: string;
  method: string;
  status: string;
  amount: number;
  reference?: string;
  date: string;
};

export type AdminReport = {
  from: string;
  to: string;
  orderCount: number;
  revenue: number;
  codOrders: number;
  cancelledOrders: number;
  salesTrend: ChartPoint[];
  topProducts: TopProduct[];
};

export type ChartPoint = { label: string; value: number; count: number };
export type TopProduct = { productId: number; name: string; quantity: number; revenue: number };
export type AdminAlert = { severity: string; type: string; message: string; link?: string };
export type AdminAudit = {
  id: number;
  userId?: number;
  action: string;
  entityType: string;
  entityId?: number;
  details?: string;
  createdAt: string;
};

export type AdminDashboard = {
  userCount: number;
  productCount: number;
  orderCount: number;
  pendingOrders: number;
  completedOrders: number;
  cancelledOrders: number;
  codOrders: number;
  revenue: number;
  successfulPayments: number;
  failedPayments: number;
  pendingPayments: number;
  lowStockCount: number;
  ordersByStatus: ChartPoint[];
  revenueTrend: ChartPoint[];
  topProducts: TopProduct[];
  lowStockProducts: AdminProduct[];
  recentOrders: AdminOrder[];
  recentRegistrations: AdminUser[];
};

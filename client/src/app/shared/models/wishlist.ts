import { Product } from './product';

export type WishlistItem = {
  productId: number;
  product?: Product;
};

export type Wishlist = {
  items: WishlistItem[];
};

import apiClient from '../api/axios';

export interface MenuItemFromApi {
  id: number;
  name: string;
  price: number;
  description: string | null;
  imageUrl: string | null;
  isAvailable: boolean;
}

export interface CreateOrderRequest {
  orderItems: { menuItemId: number; quantity: number }[];
  pickupTime: string;
  note?: string;
}

export interface OrderItemResponse {
  menuItemId: number;
  name: string;
  quantity: number;
  price: number;
}

export interface OrderResponse {
  id: number;
  items: OrderItemResponse[];
  totalPrice: number;
  pickupTime: string;
  note: string | null;
  status: string;
  createdAt: string;
}

export const getMenuItems = async (): Promise<MenuItemFromApi[]> => {
  const response = await apiClient.get<MenuItemFromApi[]>('/Menu');
  return response.data;
};

export const createOrder = async (order: CreateOrderRequest): Promise<OrderResponse> => {
  const response = await apiClient.post<OrderResponse>('/Order', order);
  return response.data;
};

export const getMyOrders = async (): Promise<OrderResponse[]> => {
  const response = await apiClient.get<OrderResponse[]>('/Order/my-orders');
  return response.data;
};

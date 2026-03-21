import apiClient from "../api/axios";

export type OrderStatus =
  | "Received"
  | "Approved"
  | "Preparing"
  | "Ready"
  | "Paid"
  | "Cancelled"
  | "NotPaid";

export interface OrderItemResponseDto {
  id: number;
  menuItemId: number;
  menuItemName: string;
  quantity: number;
  price: number;
}

export interface OrderResponseDto {
  id: number;
  orderNumber: string;
  userId: number;
  /** Kasiyer denetimi: sipariş sahibi */
  customerName?: string | null;
  customerEmail?: string | null;
  studentNo?: string | null;
  userType: "Student" | "Staff";
  totalAmount: number;
  status: OrderStatus;
  isPaid: boolean;
  createdAt: string;
  approvedAt?: string | null;
  readyAt?: string | null;
  paidAt?: string | null;
  pickupTime?: string | null;
  note?: string | null;
  orderItems: OrderItemResponseDto[];
  /** Müşterinin tüm NotPaid adedi (Ready dahil değil) */
  customerNotPaidCount: number;
  customerNotPaidTotal: number;
}

export interface CashierUnpaidRiskOverview {
  usersAtOrOverLimit: number;
  totalOpenNotPaidOrders: number;
  unpaidLimit: number;
}

export interface CreateOrderItemDto {
  menuItemId: number;
  quantity: number;
}

export interface CreateOrderDto {
  orderItems: CreateOrderItemDto[];
}

export const createOrder = async (payload: CreateOrderDto) => {
  const res = await apiClient.post("/orders", payload);
  return res.data;
};

export const getMyOrders = async (): Promise<OrderResponseDto[]> => {
  const res = await apiClient.get<OrderResponseDto[]>("/orders/my");
  return res.data;
};

export const getCashierOrders = async (params?: {
  status?: OrderStatus;
  isPaid?: boolean;
  /** 2+ karakter: ad, e-posta veya öğrenci no ile eşleşen kullanıcıların tüm sipariş geçmişi */
  userSearch?: string;
}): Promise<OrderResponseDto[]> => {
  const res = await apiClient.get<OrderResponseDto[]>("/cashier/orders", {
    params,
  });
  return res.data;
};

export const getCashierUnpaidRiskOverview =
  async (): Promise<CashierUnpaidRiskOverview> => {
    const res = await apiClient.get<CashierUnpaidRiskOverview>(
      "/cashier/orders/unpaid-risk-overview"
    );
    return res.data;
  };

export const getCashierUnpaidByUser = async (
  userId: number
): Promise<OrderResponseDto[]> => {
  const res = await apiClient.get<OrderResponseDto[]>(
    "/cashier/orders/unpaid-by-user",
    { params: { userId } }
  );
  return res.data;
};

export const cashierSettleDebt = async (id: number) => {
  const res = await apiClient.put<OrderResponseDto>(
    `/cashier/orders/${id}/settle-debt`
  );
  return res.data;
};

export const cashierSettleAllUnpaid = async (userId: number) => {
  const res = await apiClient.post<{ settledCount: number; message: string }>(
    "/cashier/orders/settle-all-unpaid",
    null,
    { params: { userId } }
  );
  return res.data;
};

export const cashierApprove = async (id: number) => {
  const res = await apiClient.put<OrderResponseDto>(
    `/cashier/orders/${id}/approve`
  );
  return res.data;
};

export const cashierPreparing = async (id: number) => {
  const res = await apiClient.put<OrderResponseDto>(
    `/cashier/orders/${id}/preparing`
  );
  return res.data;
};

export const cashierReady = async (id: number) => {
  const res = await apiClient.put<OrderResponseDto>(`/cashier/orders/${id}/ready`);
  return res.data;
};

export const cashierPaid = async (id: number) => {
  const res = await apiClient.put<OrderResponseDto>(`/cashier/orders/${id}/paid`);
  return res.data;
};

export const cashierNotPaid = async (id: number) => {
  const res = await apiClient.put<OrderResponseDto>(
    `/cashier/orders/${id}/notpaid`
  );
  return res.data;
};

export const cashierCancel = async (id: number) => {
  const res = await apiClient.put<OrderResponseDto>(
    `/cashier/orders/${id}/cancel`
  );
  return res.data;
};

export const getOrderStatusText = (status: OrderStatus) => {
  if (status === "Received") return "Sipariş alındı";
  if (status === "Approved" || status === "Preparing") return "Siparişiniz hazırlanıyor";
  if (status === "Ready") return "Siparişiniz hazır";
  if (status === "Paid") return "Sipariş teslim edildi / ödendi";
  if (status === "NotPaid") return "Ödenmedi";
  if (status === "Cancelled") return "İptal edildi";
  return status;
};


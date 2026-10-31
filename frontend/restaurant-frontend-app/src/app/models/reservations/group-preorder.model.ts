import { DishListResponse } from '@app/models/dishes/dish-list-response.model';

export interface OwnerSessionResponse {
  guestSessionId: string;
  groupReservationId: string;
  status: 'Open' | 'Submitted' | 'Closed';
}

export interface GroupOrderItem {
  dish: DishListResponse;
  quantity: number;
}

export interface GroupOrderParticipant {
  id: string | null;
  type: 'Owner' | 'Guest';
  name: string;
  items: GroupOrderItem[];
  total: number;
  isSubmitted: boolean;
}

export interface GroupOrderSnapshot {
  participants: GroupOrderParticipant[];
  total: number;
}

export interface SubmittedGroupOrderItem {
  dishId: string;
  quantity: number;
}

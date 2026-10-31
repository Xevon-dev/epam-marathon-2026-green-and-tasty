import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '@app/shared/config/api-base-url.token';
import {
  GroupOrderSnapshot,
  OwnerSessionResponse,
  SubmittedGroupOrderItem,
} from '@app/models/reservations/group-preorder.model';
import { GuestMenuResponse } from './guest-sessions.service';

@Injectable({ providedIn: 'root' })
export class GroupPreorderService {
  private readonly baseUrl = inject(API_BASE_URL);
  private readonly http = inject(HttpClient);

  getOwnerSession$(reservationId: string): Observable<OwnerSessionResponse> {
    return this.http.get<OwnerSessionResponse>(
      `${this.baseUrl}/reservations/${encodeURIComponent(reservationId)}/guest-session-id`,
    );
  }

  getSessions$(groupReservationId: string): Observable<GroupOrderSnapshot> {
    return this.http.get<GroupOrderSnapshot>(
      `${this.baseUrl}/guest/groups/${encodeURIComponent(groupReservationId)}/sessions`,
    );
  }

  getMenu$(groupReservationId: string): Observable<GuestMenuResponse> {
    return this.http.get<GuestMenuResponse>(
      `${this.baseUrl}/group-reservations/${encodeURIComponent(groupReservationId)}/owner-session/menu`,
    );
  }

  addOwnerDish$(groupReservationId: string, dishId: string, quantity = 1): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(
      `${this.baseUrl}/group-reservations/${encodeURIComponent(groupReservationId)}/owner-session/dishes`,
      { dishId, quantity },
    );
  }

  removeOwnerDish$(groupReservationId: string, dishId: string): Observable<{ message: string }> {
    return this.http.delete<{ message: string }>(
      `${this.baseUrl}/group-reservations/${encodeURIComponent(groupReservationId)}/owner-session/dishes/${encodeURIComponent(dishId)}`,
    );
  }

  submitOwnerSession$(groupReservationId: string): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(
      `${this.baseUrl}/group-reservations/${encodeURIComponent(groupReservationId)}/owner-session/submit`,
      {},
    );
  }

  removeGuest$(groupReservationId: string, guestSessionId: string): Observable<GroupOrderSnapshot> {
    return this.http.delete<GroupOrderSnapshot>(
      `${this.baseUrl}/guest/groups/${encodeURIComponent(groupReservationId)}/sessions/${encodeURIComponent(guestSessionId)}`,
    );
  }

  submitGroup$(groupReservationId: string, reservationId: string): Observable<SubmittedGroupOrderItem[]> {
    return this.http.post<SubmittedGroupOrderItem[]>(
      `${this.baseUrl}/group-reservations/${encodeURIComponent(groupReservationId)}/submit`,
      { reservationId },
    );
  }
}

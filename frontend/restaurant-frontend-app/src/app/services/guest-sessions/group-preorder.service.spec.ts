import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { API_BASE_URL } from '@app/shared/config/api-base-url.token';
import { GroupPreorderService } from './group-preorder.service';

describe('GroupPreorderService', () => {
  let service: GroupPreorderService;
  let httpMock: HttpTestingController;
  const baseUrl = 'https://restaurant.test/develop';

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        GroupPreorderService,
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: API_BASE_URL, useValue: baseUrl },
      ],
    });
    service = TestBed.inject(GroupPreorderService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('requests the owner session for a reservation', () => {
    service.getOwnerSession$('reservation-1').subscribe();
    const request = httpMock.expectOne(`${baseUrl}/reservations/reservation-1/guest-session-id`);
    expect(request.request.method).toBe('GET');
    request.flush({ guestSessionId: 'owner-1', groupReservationId: 'group-1', status: 'Open' });
  });

  it('loads the owner-only group snapshot', () => {
    service.getSessions$('group-1').subscribe();
    const request = httpMock.expectOne(`${baseUrl}/guest/groups/group-1/sessions`);
    expect(request.request.method).toBe('GET');
    request.flush({ participants: [], total: 0 });
  });

  it('loads the owner menu through the Cognito-protected owner route', () => {
    service.getMenu$('group-1').subscribe();
    const request = httpMock.expectOne(`${baseUrl}/group-reservations/group-1/owner-session/menu`);
    expect(request.request.method).toBe('GET');
    request.flush({ dishes: {} });
  });

  it('submits the consolidated group order with its reservation id', () => {
    service.submitGroup$('group-1', 'reservation-1').subscribe();
    const request = httpMock.expectOne(`${baseUrl}/group-reservations/group-1/submit`);
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ reservationId: 'reservation-1' });
    request.flush([{ dishId: 'dish-1', quantity: 3 }]);
  });

  it('removes a guest session through the owner route', () => {
    service.removeGuest$('group-1', 'guest-1').subscribe();
    const request = httpMock.expectOne(`${baseUrl}/guest/groups/group-1/sessions/guest-1`);
    expect(request.request.method).toBe('DELETE');
    request.flush({ participants: [], total: 0 });
  });
});

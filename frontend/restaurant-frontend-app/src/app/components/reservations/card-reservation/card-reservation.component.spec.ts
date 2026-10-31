import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { ReservationCustomer, ReservationStatusCustomer } from '@app/models/reservations/reservation.model';
import { CardReservationComponent } from './card-reservation.component';

describe('CardReservationComponent', () => {
  let component: CardReservationComponent;
  let fixture: ComponentFixture<CardReservationComponent>;
  let router: Router;

  const reservation: ReservationCustomer = {
    id: 'reservation-1',
    tableId: 'table-1',
    startTime: new Date('2026-11-10T18:00:00Z'),
    endTime: new Date('2026-11-10T19:30:00Z'),
    guestCount: 3,
    status: ReservationStatusCustomer.Reserved,
    restaurantName: 'Green & Tasty',
    locationAddress: '48 Rustaveli Avenue',
    waiterId: 'waiter-1',
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      providers: [provideRouter([])],
      imports: [CardReservationComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(CardReservationComponent);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('r', reservation);
    fixture.detectChanges();
    router = TestBed.inject(Router);
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('shows a group-order action for a multi-guest customer reservation', () => {
    const button = fixture.nativeElement.querySelector('button') as HTMLButtonElement;
    expect(fixture.nativeElement.textContent).toContain('Group order');
    expect(button).toBeTruthy();
  });

  it('navigates to the group-order page for the selected reservation', () => {
    const navigateSpy = spyOn(router, 'navigate').and.resolveTo(true);

    component.onOpenGroupOrder();

    expect(navigateSpy).toHaveBeenCalledOnceWith(['/group-order', 'reservation-1']);
  });

  it('does not show group-order for a single-guest reservation', () => {
    fixture.componentRef.setInput('r', { ...reservation, guestCount: 1 });
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).not.toContain('Group order');
  });
});

import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { GroupOrderSnapshot } from '@app/models/reservations/group-preorder.model';
import { GroupPreorderService } from '@app/services/guest-sessions/group-preorder.service';
import { GroupOrderPageComponent } from './group-order-page.component';

describe('GroupOrderPageComponent', () => {
  let fixture: ComponentFixture<GroupOrderPageComponent>;
  let component: GroupOrderPageComponent;
  let groupPreorderService: jasmine.SpyObj<GroupPreorderService>;

  const initialSnapshot: GroupOrderSnapshot = {
    participants: [
      { id: 'owner-1', type: 'Owner', name: 'You (owner)', items: [], total: 0, isSubmitted: false },
      { id: 'guest-1', type: 'Guest', name: 'Marta', items: [], total: 24, isSubmitted: true },
    ],
    total: 24,
  };

  beforeEach(() => {
    groupPreorderService = jasmine.createSpyObj<GroupPreorderService>('GroupPreorderService', [
      'getOwnerSession$',
      'getSessions$',
      'getMenu$',
      'addOwnerDish$',
      'removeOwnerDish$',
      'submitOwnerSession$',
      'removeGuest$',
      'submitGroup$',
    ]);
    groupPreorderService.getOwnerSession$.and.returnValue(of({
      guestSessionId: 'owner-1',
      groupReservationId: 'group-1',
      status: 'Open',
    }));
    groupPreorderService.getSessions$.and.returnValue(of(initialSnapshot));
    groupPreorderService.getMenu$.and.returnValue(of({ dishes: {} }));
    groupPreorderService.submitGroup$.and.returnValue(of([{ dishId: 'dish-1', quantity: 2 }]));

    TestBed.configureTestingModule({
      imports: [GroupOrderPageComponent],
      providers: [
        provideRouter([]),
        { provide: GroupPreorderService, useValue: groupPreorderService },
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { paramMap: { get: () => 'reservation-1' } } },
        },
      ],
    });

    fixture = TestBed.createComponent(GroupOrderPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('loads owner data and displays participant totals', () => {
    expect(groupPreorderService.getOwnerSession$).toHaveBeenCalledOnceWith('reservation-1');
    expect(component.snapshot?.total).toBe(24);
    expect(fixture.nativeElement.textContent).toContain('Marta');
    expect(fixture.nativeElement.textContent).toContain('Your group order');
  });

  it('replaces the participant list with the server snapshot after removing a guest', () => {
    const updatedSnapshot: GroupOrderSnapshot = {
      participants: [initialSnapshot.participants[0]],
      total: 0,
    };
    groupPreorderService.removeGuest$.and.returnValue(of(updatedSnapshot));

    component.removeGuest(initialSnapshot.participants[1]);

    expect(groupPreorderService.removeGuest$).toHaveBeenCalledOnceWith('group-1', 'guest-1');
    expect(component.snapshot).toEqual(updatedSnapshot);
  });

  it('submits only when there is a submitted non-empty participant basket', () => {
    expect(component.canSubmitGroup).toBeTrue();

    component.submitGroupOrder();

    expect(groupPreorderService.submitGroup$).toHaveBeenCalledOnceWith('group-1', 'reservation-1');
    expect(component.isGroupSubmitted).toBeTrue();
    expect(component.submittedItems).toEqual([{ dishId: 'dish-1', quantity: 2 }]);
  });

  it('shows a useful error when group data cannot be loaded', () => {
    groupPreorderService.getSessions$.and.returnValue(throwError(() => new Error('offline')));

    component.load();

    expect(component.errorMessage).toContain('Could not load group-order participants');
    expect(component.isLoading).toBeFalse();
  });
});

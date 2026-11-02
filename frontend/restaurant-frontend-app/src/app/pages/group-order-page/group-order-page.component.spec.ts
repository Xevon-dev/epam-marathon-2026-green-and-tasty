import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpErrorResponse } from '@angular/common/http';
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
    expect(component.isLoading).toBeFalse();
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

  it('clears the group submit state and guides the owner when no pre-order exists', () => {
    groupPreorderService.submitGroup$.and.returnValue(
      throwError(
        () =>
          new HttpErrorResponse({
            status: 400,
            statusText: 'Bad Request',
            error: { error: 'Pre-order for the reservation was not found.' },
          }),
      ),
    );

    component.submitGroupOrder();

    expect(component.isSubmittingGroup).toBeFalse();
    expect(component.errorMessage).toContain('Create and submit a regular pre-order');
  });

  it('shows a useful error when group data cannot be loaded', () => {
    groupPreorderService.getSessions$.and.returnValue(throwError(() => new Error('offline')));

    component.load();

    expect(component.errorMessage).toContain('Could not load group-order participants');
    expect(component.isLoading).toBeFalse();
  });

  it('adds and removes owner dishes through the group service', () => {
    groupPreorderService.addOwnerDish$.and.returnValue(of({ message: 'added' }));
    groupPreorderService.removeOwnerDish$.and.returnValue(of({ message: 'removed' }));

    component.addOwnerDish({ id: 'dish-1', name: 'Soup', price: 10, selectable: true } as any);
    expect(groupPreorderService.addOwnerDish$).toHaveBeenCalledOnceWith('group-1', 'dish-1');

    component.removeOwnerDish('dish-1');
    expect(groupPreorderService.removeOwnerDish$).toHaveBeenCalledOnceWith('group-1', 'dish-1');
  });

  it('submits the owner basket when it contains dishes', () => {
    const ownerDish = { id: 'dish-1', name: 'Soup', price: 10, selectable: true } as any;
    const snapshot: GroupOrderSnapshot = {
      participants: [
        {
          id: 'owner-1',
          type: 'Owner',
          name: 'You (owner)',
          items: [{ dish: ownerDish, quantity: 1 }],
          total: 10,
          isSubmitted: false,
        },
        initialSnapshot.participants[1],
      ],
      total: 34,
    };
    groupPreorderService.getSessions$.and.returnValue(of(snapshot));
    groupPreorderService.submitOwnerSession$.and.returnValue(of({ message: 'submitted' }));

    component.load();
    component.submitOwnerSession();

    expect(groupPreorderService.submitOwnerSession$).toHaveBeenCalledOnceWith('group-1');
  });
});

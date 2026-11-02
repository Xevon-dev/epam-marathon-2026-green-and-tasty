import { ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { CurrencyPipe } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { catchError, forkJoin, finalize, of, switchMap } from 'rxjs';
import {
  GroupOrderParticipant,
  GroupOrderSnapshot,
  OwnerSessionResponse,
  SubmittedGroupOrderItem,
} from '@app/models/reservations/group-preorder.model';
import { GuestMenuDish } from '@app/services/guest-sessions/guest-sessions.service';
import { GroupPreorderService } from '@app/services/guest-sessions/group-preorder.service';

@Component({
  selector: 'app-group-order-page',
  standalone: true,
  imports: [RouterLink, CurrencyPipe],
  templateUrl: './group-order-page.component.html',
  styleUrl: './group-order-page.component.scss',
})
export class GroupOrderPageComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly groupPreorderService = inject(GroupPreorderService);
  private readonly changeDetector = inject(ChangeDetectorRef);

  reservationId = '';
  ownerSession: OwnerSessionResponse | null = null;
  snapshot: GroupOrderSnapshot | null = null;
  menuDishes: GuestMenuDish[] = [];
  submittedItems: SubmittedGroupOrderItem[] = [];
  groupSubmitted = false;
  isLoading = true;
  isSubmittingOwner = false;
  isSubmittingGroup = false;
  errorMessage = '';
  successMessage = '';

  get ownerParticipant(): GroupOrderParticipant | undefined {
    return this.snapshot?.participants.find((participant) => participant.type === 'Owner');
  }

  get submittedParticipantCount(): number {
    return this.snapshot?.participants.filter((participant) => participant.isSubmitted).length ?? 0;
  }

  get submittedTotal(): number {
    return this.snapshot?.participants
      .filter((participant) => participant.isSubmitted)
      .reduce((total, participant) => total + participant.total, 0) ?? 0;
  }

  get canSubmitGroup(): boolean {
    return Boolean(this.snapshot && this.submittedParticipantCount > 0 && this.submittedTotal > 0);
  }

  get isGroupSubmitted(): boolean {
    return this.groupSubmitted;
  }

  ngOnInit(): void {
    this.reservationId = this.route.snapshot.paramMap.get('reservationId') ?? '';
    this.load();
  }

  load(): void {
    if (!this.reservationId) {
      this.errorMessage = 'No reservation was provided for this group order.';
      this.isLoading = false;
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';
    this.changeDetector.markForCheck();

    this.groupPreorderService
      .getOwnerSession$(this.reservationId)
      .pipe(
        switchMap((ownerSession) => {
          this.ownerSession = ownerSession;
          this.groupSubmitted = ownerSession.status === 'Submitted';
          if (this.groupSubmitted) {
            this.successMessage = 'This group pre-order has already been sent to the restaurant.';
          }

          const menu$ = ownerSession.status === 'Submitted'
            ? of(null)
            : this.groupPreorderService.getMenu$(ownerSession.groupReservationId).pipe(
                catchError(() => {
                  this.errorMessage = 'Could not load the menu for this group order.';
                  return of(null);
                }),
              );

          return forkJoin({
            snapshot: this.groupPreorderService.getSessions$(ownerSession.groupReservationId),
            menu: menu$,
          });
        }),
        finalize(() => {
          this.isLoading = false;
          this.changeDetector.markForCheck();
        }),
      )
      .subscribe({
        next: ({ snapshot, menu }) => {
          if (!snapshot || !Array.isArray(snapshot.participants)) {
            this.errorMessage = 'The server returned an invalid group-order snapshot.';
            return;
          }

          this.snapshot = snapshot;
          this.menuDishes = menu?.dishes ? Object.values(menu.dishes).flat() : [];
          this.changeDetector.markForCheck();
        },
        error: () => {
          this.errorMessage = this.ownerSession
            ? 'Could not load group-order participants.'
            : 'Could not open the group order. Make sure the reservation is active.';
          this.changeDetector.markForCheck();
        },
      });
  }

  addOwnerDish(dish: GuestMenuDish): void {
    if (!this.ownerSession || !this.snapshot || this.ownerParticipant?.isSubmitted || !dish.selectable) return;

    this.errorMessage = '';
    this.groupPreorderService
      .addOwnerDish$(this.ownerSession.groupReservationId, dish.id)
      .subscribe({
        next: () => this.refreshSnapshot(),
        error: () => {
          this.errorMessage = `Could not add ${dish.name} to your order.`;
        },
      });
  }

  removeOwnerDish(dishId: string): void {
    if (!this.ownerSession || this.ownerParticipant?.isSubmitted) return;
    this.groupPreorderService
      .removeOwnerDish$(this.ownerSession.groupReservationId, dishId)
      .subscribe({
        next: () => this.refreshSnapshot(),
        error: () => {
          this.errorMessage = 'Could not remove the dish from your order.';
        },
      });
  }

  submitOwnerSession(): void {
    if (!this.ownerSession || !this.ownerParticipant?.items.length) return;
    this.isSubmittingOwner = true;
    this.errorMessage = '';
    this.groupPreorderService.submitOwnerSession$(this.ownerSession.groupReservationId).subscribe({
      next: () => {
        this.isSubmittingOwner = false;
        this.refreshSnapshot();
      },
      error: () => {
        this.isSubmittingOwner = false;
        this.errorMessage = 'Could not submit your dishes.';
      },
    });
  }

  removeGuest(participant: GroupOrderParticipant): void {
    if (!this.ownerSession || participant.type !== 'Guest' || !participant.id || this.isGroupSubmitted) return;
    this.errorMessage = '';
    this.groupPreorderService.removeGuest$(this.ownerSession.groupReservationId, participant.id).subscribe({
      next: (snapshot) => (this.snapshot = snapshot),
      error: () => {
        this.errorMessage = `Could not remove ${participant.name} from the group order.`;
      },
    });
  }

  submitGroupOrder(): void {
    if (!this.ownerSession || !this.canSubmitGroup || this.isGroupSubmitted) return;
    this.isSubmittingGroup = true;
    this.errorMessage = '';
    this.groupPreorderService
      .submitGroup$(this.ownerSession.groupReservationId, this.reservationId)
      .subscribe({
      next: (items) => {
          this.submittedItems = items;
          this.groupSubmitted = true;
          this.successMessage = 'The combined pre-order was sent to the restaurant.';
          this.isSubmittingGroup = false;
        },
        error: () => {
          this.isSubmittingGroup = false;
          this.errorMessage = 'Could not submit the group order. Check that at least one participant submitted a non-empty cart.';
        },
      });
  }

  canEditOwnerDish(participant: GroupOrderParticipant): boolean {
    return participant.type === 'Owner' && !participant.isSubmitted && !this.isGroupSubmitted;
  }

  private refreshSnapshot(): void {
    if (!this.ownerSession) return;
    this.groupPreorderService.getSessions$(this.ownerSession.groupReservationId).subscribe({
      next: (snapshot) => (this.snapshot = snapshot),
      error: () => {
        this.errorMessage = 'The order was updated, but the participant list could not be refreshed.';
      },
    });
  }
}

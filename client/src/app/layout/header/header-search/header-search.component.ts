import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { MatIconButton } from '@angular/material/button';
import { MatIcon } from '@angular/material/icon';

@Component({
  selector: 'app-header-search',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, MatIconButton, MatIcon],
  template: `
  <form (submit)="onSearch($event)" class="hidden lg:flex items-center gap-2">
    <input
      type="search"
      [(ngModel)]="searchTerm"
      name="search"
      placeholder="Search products..."
      class="px-3 py-2 border border-gray-300 rounded-lg text-sm w-48 xl:w-64"
    />
    <button mat-icon-button type="submit" aria-label="Search">
      <mat-icon>search</mat-icon>
    </button>
  </form>
  `,
})
export class HeaderSearchComponent {
  private router = inject(Router);
  searchTerm = '';

  onSearch(event: Event) {
    event.preventDefault();
    if (this.searchTerm.trim()) {
      this.router.navigate(['/shop'], { queryParams: { search: this.searchTerm.trim() } });
    }
  }
}

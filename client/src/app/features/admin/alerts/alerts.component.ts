import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { MatButton } from '@angular/material/button';
import { RouterLink } from '@angular/router';
import { AdminService } from '../../../core/services/admin.service';
import { AdminAlert } from '../../../shared/models/admin';

@Component({
  selector: 'app-admin-alerts',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatButton, RouterLink],
  template: `
    <div class="max-w-4xl mx-auto py-8 px-4">
      <div class="flex justify-between mb-5"><h1 class="text-2xl font-bold">Operational Alerts</h1><a mat-stroked-button routerLink="/admin">Dashboard</a></div>
      @for (a of alerts(); track a.type + a.message) {
        <div class="border-l-4 rounded p-4 mb-3" [class]="a.severity === 'Critical' ? 'border-red-600 bg-red-50' : a.severity === 'Warning' ? 'border-amber-500 bg-amber-50' : 'border-blue-500 bg-blue-50'">
          <strong>{{ a.type }}</strong><p>{{ a.message }}</p>@if (a.link) { <a [routerLink]="a.link" class="text-blue-700 underline">Review</a> }
        </div>
      } @empty { <p class="text-gray-500">No operational alerts.</p> }
    </div>
  `,
})
export class AdminAlertsComponent implements OnInit {
  private admin = inject(AdminService);
  alerts = signal<AdminAlert[]>([]);
  ngOnInit() { this.admin.getAlerts().subscribe(x => this.alerts.set(x)); }
}

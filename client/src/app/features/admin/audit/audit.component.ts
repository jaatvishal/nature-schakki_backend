import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButton } from '@angular/material/button';
import { RouterLink } from '@angular/router';
import { AdminService } from '../../../core/services/admin.service';
import { AdminAudit } from '../../../shared/models/admin';

@Component({
  selector: 'app-admin-audit',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, MatButton, RouterLink, DatePipe],
  template: `
    <div class="max-w-6xl mx-auto py-8 px-4">
      <div class="flex justify-between mb-5"><h1 class="text-2xl font-bold">Admin Audit Log</h1><a mat-stroked-button routerLink="/admin">Dashboard</a></div>
      <div class="flex gap-2 mb-4"><input [(ngModel)]="search" (keyup.enter)="load()" placeholder="Search action or entity" class="border rounded px-3 py-2 flex-1"><button mat-flat-button (click)="load()">Search</button></div>
      <table class="w-full text-sm"><thead><tr class="border-b bg-gray-50"><th class="p-3 text-left">When</th><th>Admin</th><th>Action</th><th>Entity</th><th>Details</th></tr></thead>
        <tbody>@for (a of records(); track a.id) {
          <tr class="border-b"><td class="p-3">{{ a.createdAt | date:'short' }}</td><td class="text-center">#{{ a.userId }}</td><td>{{ a.action }}</td>
            <td>{{ a.entityType }} @if (a.entityId) { #{{ a.entityId }} }</td><td>{{ a.details || '—' }}</td></tr>
        } @empty { <tr><td colspan="5" class="p-8 text-center text-gray-500">No audit entries.</td></tr> }</tbody></table>
    </div>
  `,
})
export class AdminAuditComponent implements OnInit {
  private admin = inject(AdminService);
  records = signal<AdminAudit[]>([]);
  search = '';
  ngOnInit() { this.load(); }
  load() { this.admin.getAudit(this.search).subscribe(x => this.records.set(x.items)); }
}

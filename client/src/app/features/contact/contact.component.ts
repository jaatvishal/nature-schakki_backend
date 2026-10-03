import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatIcon } from '@angular/material/icon';

@Component({
  selector: 'app-contact',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, MatIcon],
  template: `
    <div class="max-w-4xl mx-auto py-12 px-4">
      <h1 class="text-3xl font-bold mb-2">Contact Us</h1>
      <p class="text-gray-600 mb-10">We'd love to hear from you. Reach out anytime.</p>

      <div class="grid md:grid-cols-2 gap-8">
        <div class="space-y-6">
          @for (item of contactInfo; track item.label) {
            <div class="flex gap-4 items-start p-4 border rounded-lg hover:shadow-md transition">
              <mat-icon class="text-amber-600">{{ item.icon }}</mat-icon>
              <div>
                <p class="font-semibold">{{ item.label }}</p>
                @if (item.link) {
                  <a [href]="item.link" class="text-gray-600 hover:text-amber-600">{{ item.value }}</a>
                } @else {
                  <p class="text-gray-600">{{ item.value }}</p>
                }
              </div>
            </div>
          }
        </div>

        <div class="bg-amber-50 p-6 rounded-xl">
          <h2 class="font-bold text-lg mb-4">Follow Us</h2>
          <div class="flex flex-wrap gap-3">
            @for (s of socials; track s.url) {
              <a [href]="s.url" target="_blank" rel="noopener"
                class="flex items-center gap-2 px-4 py-2 bg-white rounded-lg border hover:border-amber-500 transition">
                <mat-icon class="text-amber-600 text-base">{{ s.icon }}</mat-icon>
                <span class="text-sm">{{ s.name }}</span>
              </a>
            }
          </div>
          <p class="text-sm text-gray-600 mt-6">Business hours: Mon–Sat, 9 AM – 6 PM IST</p>
          <a routerLink="/shop" class="inline-block mt-4 text-amber-700 font-medium hover:underline">Browse Products →</a>
        </div>
      </div>
    </div>
  `,
})
export class ContactComponent {
  contactInfo = [
    { icon: 'email', label: 'Email', value: 'info@natureschakki.in', link: 'mailto:info@natureschakki.in' },
    { icon: 'phone', label: 'Phone', value: '+91 9870514837', link: 'tel:+919870514837' },
    { icon: 'phone', label: 'Alternate Phone', value: '+91 9818213553', link: 'tel:+919818213553' },
    { icon: 'location_on', label: 'Address', value: 'P.No-11, Senga Enclave, Girdharpur Road, Chhapraula, G.B. Nagar 201009', link: null },
  ];
  socials = [
    { name: 'Facebook', icon: 'facebook', url: 'https://www.facebook.com/share/1C4tTjRKgx/?mibextid=wwXIfr' },
    { name: 'Instagram', icon: 'photo_camera', url: 'https://instagram.com/natureschakki' },
    { name: 'Twitter', icon: 'alternate_email', url: 'https://twitter.com/natureschakki' },
    { name: 'LinkedIn', icon: 'business', url: 'https://linkedin.com/company/natureschakki' },
  ];
}

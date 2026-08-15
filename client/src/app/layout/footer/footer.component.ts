import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatIcon } from '@angular/material/icon';

@Component({
  selector: 'app-footer',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, MatIcon],
  template: `
    <footer class="bg-gray-900 text-gray-300 mt-16">
      <div class="max-w-screen-2xl mx-auto px-4 py-12 grid grid-cols-1 md:grid-cols-3 gap-8">
        <div>
          <h3 class="text-white font-bold text-lg mb-3">Natures Chakki</h3>
          <p class="text-sm leading-relaxed">Premium artisan flour & grains. Fresh, natural, delivered to your door.</p>
        </div>
        <div>
          <h3 class="text-white font-semibold mb-3">Quick Links</h3>
          <ul class="space-y-2 text-sm">
            <li><a routerLink="/shop" class="hover:text-amber-400">Shop</a></li>
            <li><a routerLink="/contact" class="hover:text-amber-400">Contact</a></li>
            <li><a routerLink="/auth/login" class="hover:text-amber-400">Login</a></li>
          </ul>
        </div>
        <div>
          <h3 class="text-white font-semibold mb-3">Connect</h3>
          <div class="flex gap-4">
            @for (s of socials; track s.url) {
              <a [href]="s.url" target="_blank" rel="noopener" [attr.aria-label]="s.label"
                class="w-10 h-10 rounded-full bg-gray-800 flex items-center justify-center hover:bg-amber-600 transition">
                <mat-icon class="text-lg">{{ s.icon }}</mat-icon>
              </a>
            }
          </div>
          <p class="text-sm mt-4">support&#64;natureschakki.com</p>
          <p class="text-sm">+91 98765 43210</p>
        </div>
      </div>
      <div class="border-t border-gray-800 text-center text-xs py-4">
        &copy; {{ year }} Natures Chakki. All rights reserved.
      </div>
    </footer>
  `,
})
export class FooterComponent {
  year = new Date().getFullYear();
  socials = [
    { icon: 'facebook', label: 'Facebook', url: 'https://facebook.com/natureschakki' },
    { icon: 'photo_camera', label: 'Instagram', url: 'https://instagram.com/natureschakki' },
    { icon: 'alternate_email', label: 'Twitter', url: 'https://twitter.com/natureschakki' },
    { icon: 'business', label: 'LinkedIn', url: 'https://linkedin.com/company/natureschakki' },
  ];
}

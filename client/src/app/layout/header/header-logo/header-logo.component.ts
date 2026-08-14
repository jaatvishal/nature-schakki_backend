import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-header-logo',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink],
  template: `
    <a routerLink="/" class="block">
      <img src="/images/logo.png" alt="Natures Chakki" class="max-h-16" />
    </a>
  `,
})
export class HeaderLogoComponent {}

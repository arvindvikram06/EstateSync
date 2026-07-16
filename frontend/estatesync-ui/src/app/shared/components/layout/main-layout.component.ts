import { Component, ChangeDetectionStrategy } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { NavbarComponent } from '../navbar/navbar.component';
import { FooterComponent } from './footer.component';

@Component({
  selector: 'app-main-layout',
  standalone: true,
  imports: [RouterOutlet, NavbarComponent, FooterComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './main-layout.component.html'
})
export class MainLayoutComponent {}

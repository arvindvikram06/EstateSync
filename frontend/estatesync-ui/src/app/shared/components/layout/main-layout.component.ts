import { Component, ChangeDetectionStrategy } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { NavbarComponent } from '../navbar/navbar.component';

@Component({
  selector: 'app-main-layout',
  standalone: true,
  imports: [RouterOutlet, NavbarComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './main-layout.component.html'
})
export class MainLayoutComponent {}

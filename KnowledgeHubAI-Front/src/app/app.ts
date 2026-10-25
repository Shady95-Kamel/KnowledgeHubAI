
import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { AuthSessionService } from '../app/core/auth/auth-session';

@Component({
  imports: [RouterOutlet],
  selector: 'app-root',
  styleUrl: './app.scss',
  templateUrl: './app.html',
})
export class App implements OnInit {
  protected readonly title = signal('knowledge-hub-ai-ui');

  private readonly authSession = inject(AuthSessionService);

  ngOnInit(): void {
    this.authSession.initialize();
  }
}

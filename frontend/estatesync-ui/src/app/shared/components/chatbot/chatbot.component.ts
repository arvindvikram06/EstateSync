import {
  Component, ChangeDetectionStrategy, signal, computed,
  inject, ViewChild, ElementRef, AfterViewChecked
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { ChatbotService } from '../../../core/services/shared/chatbot.service';

interface ChatMessage {
  role: 'user' | 'assistant';
  text: string;
}

@Component({
  selector: 'app-chatbot',
  standalone: true,
  imports: [CommonModule, FormsModule, MatIconModule, MatButtonModule, MatProgressSpinnerModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './chatbot.component.html',
  styleUrl: './chatbot.component.css',
})
export class ChatbotComponent implements AfterViewChecked {
  @ViewChild('messagesList') private messagesList!: ElementRef<HTMLDivElement>;

  readonly chatbotService = inject(ChatbotService);

  readonly isOpen = signal(false);
  readonly messages = signal<ChatMessage[]>([]);
  inputText = '';
  private shouldScroll = false;

  readonly hasMessages = computed(() => this.messages().length > 0);

  toggle(): void {
    this.isOpen.update(v => !v);
  }

  sendMessage(): void {
    const text = this.inputText.trim();
    if (!text || this.chatbotService.isLoading()) return;

    this.messages.update(msgs => [...msgs, { role: 'user', text }]);
    this.inputText = '';
    this.shouldScroll = true;

    this.chatbotService.chat(text).subscribe({
      next: res => {
        this.messages.update(msgs => [...msgs, { role: 'assistant', text: res.data ?? '' }]);
        this.shouldScroll = true;
      },
      error: () => {
        this.messages.update(msgs => [
          ...msgs,
          { role: 'assistant', text: 'Something went wrong. Please try again.' },
        ]);
        this.shouldScroll = true;
      },
    });
  }

  ngAfterViewChecked(): void {
    if (this.shouldScroll && this.messagesList) {
      const el = this.messagesList.nativeElement;
      el.scrollTop = el.scrollHeight;
      this.shouldScroll = false;
    }
  }
}

import {
  Component, ChangeDetectionStrategy, signal, computed,
  inject, ViewChild, ElementRef, AfterViewChecked, OnDestroy
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { Router } from '@angular/router';
import {
  AiChatHistoryTurn,
  AiNavigationAction,
  ChatbotService,
} from '../../../core/services/shared/chatbot.service';

interface ChatMessage {
  role: 'user' | 'assistant';
  text: string;
  actions?: AiNavigationAction[];
  isStreaming?: boolean;
}

@Component({
  selector: 'app-chatbot',
  standalone: true,
  imports: [CommonModule, FormsModule, MatIconModule, MatButtonModule, MatProgressSpinnerModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './chatbot.component.html',
  styleUrl: './chatbot.component.css',
})
export class ChatbotComponent implements AfterViewChecked, OnDestroy {
  @ViewChild('messagesList') private messagesList!: ElementRef<HTMLDivElement>;

  readonly chatbotService = inject(ChatbotService);
  private readonly router = inject(Router);

  readonly isOpen = signal(false);
  readonly messages = signal<ChatMessage[]>([]);
  readonly isTyping = signal(false);
  readonly isBusy = computed(() => this.chatbotService.isLoading() || this.isTyping());
  inputText = '';
  private shouldScroll = false;
  private typewriterTimer?: ReturnType<typeof setTimeout>;

  readonly hasMessages = computed(() => this.messages().length > 0);

  toggle(): void {
    this.isOpen.update(v => !v);
  }

  sendMessage(): void {
    const text = this.inputText.trim();
    if (!text || this.isBusy()) return;

    const history: AiChatHistoryTurn[] = this.messages()
      .slice(-10)
      .map(message => ({ role: message.role, content: message.text }));
    this.messages.update(msgs => [...msgs, { role: 'user', text }]);
    this.inputText = '';
    this.shouldScroll = true;

    this.chatbotService.chat(text, history).subscribe({
      next: res => {
        const answer = res.data;
        this.revealAnswer(
          answer?.reply ?? '',
          answer?.actions ?? [],
        );
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

  private revealAnswer(
    reply: string,
    actions: AiNavigationAction[],
  ): void {
    const words = reply.match(/\S+\s*/g) ?? [];
    const messageIndex = this.messages().length;

    this.isTyping.set(true);
    this.messages.update(messages => [...messages, {
      role: 'assistant',
      text: '',
      isStreaming: true,
    }]);

    let wordIndex = 0;
    const revealNext = (): void => {
      if (wordIndex >= words.length) {
        this.messages.update(messages => messages.map((message, index) =>
          index === messageIndex
            ? { ...message, text: reply, actions, isStreaming: false }
            : message
        ));
        this.isTyping.set(false);
        this.typewriterTimer = undefined;
        this.shouldScroll = true;
        return;
      }

      const nextText = words.slice(0, ++wordIndex).join('');
      this.messages.update(messages => messages.map((message, index) =>
        index === messageIndex ? { ...message, text: nextText } : message
      ));
      this.shouldScroll = true;
      this.typewriterTimer = setTimeout(revealNext, 28);
    };

    revealNext();
  }

  openAction(action: AiNavigationAction): void {
    void this.router.navigateByUrl(action.route);
    this.isOpen.set(false);
  }

  ngAfterViewChecked(): void {
    if (this.shouldScroll && this.messagesList) {
      const el = this.messagesList.nativeElement;
      el.scrollTop = el.scrollHeight;
      this.shouldScroll = false;
    }
  }

  ngOnDestroy(): void {
    if (this.typewriterTimer) clearTimeout(this.typewriterTimer);
  }
}

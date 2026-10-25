
import {
  ChangeDetectorRef,
  Component,
  OnInit,
  inject
} from '@angular/core';

import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs';
import { RouterLink } from '@angular/router';

import {
  ChatService,
  ChatSource
} from '../../../../core/services/chat';

import {
  DocumentsService,
  DocumentDto
} from '../../../../core/services/documents';

interface ChatMessage {
  role: 'user' | 'assistant';
  content: string;
  sources?: ChatSource[];
}

@Component({
  selector: 'app-chat',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './chat.html',
  styleUrl: './chat.scss'
})
export class Chat implements OnInit {
  private readonly chatService = inject(ChatService);
  private readonly documentsService = inject(DocumentsService);
  private readonly cdr = inject(ChangeDetectorRef);

  question = '';
  selectedDocumentId: string | null = null;

  documents: DocumentDto[] = [];
  isLoadingDocuments = false;
  documentsErrorMessage = '';

  messages: ChatMessage[] = [];
  isLoading = false;
  errorMessage = '';

  ngOnInit(): void {
    this.loadDocuments();
  }

  loadDocuments(): void {
    this.isLoadingDocuments = true;
    this.documentsErrorMessage = '';

    this.documentsService.getMyDocuments({
      pageNumber: 1,
      pageSize: 100,
      descending: true,
      sortBy: 'createdAt'
    })
    .pipe(
      finalize(() => {
        this.isLoadingDocuments = false;
        this.cdr.detectChanges();
      })
    )
    .subscribe({
      next: response => {
        if (!response?.success || !response.data) {
          this.documentsErrorMessage =
            'Could not load your documents.';
          return;
        }

        // Chat search requires documents whose processing is complete.
        this.documents = response.data.items.filter(
          document => this.isDocumentReady(document.status)
        );

        this.cdr.detectChanges();
      },
      error: error => {
        console.error('Failed to load documents:', error);

        this.documentsErrorMessage =
          error.status === 401
            ? 'Your session has expired. Please sign in again.'
            : 'Could not load your documents. Please try again.';

        this.cdr.detectChanges();
      }
    });
  }

  private isDocumentReady(status: number | string): boolean {
    if (typeof status === 'number') {
      // DocumentStatus.Ready = 3 in the backend.
      return status === 3;
    }

    return status.toLowerCase() === 'ready';
  }

  sendMessage(): void {
    const question = this.question.trim();

    if (!question || this.isLoading) {
      return;
    }

    this.messages.push({
      role: 'user',
      content: question
    });

    this.question = '';
    this.errorMessage = '';
    this.isLoading = true;

    this.cdr.detectChanges();

    this.chatService.sendMessage({
      question,
      documentId: this.selectedDocumentId,
      limit: 5
    })
    .pipe(
      finalize(() => {
        this.isLoading = false;
        this.cdr.detectChanges();
      })
    )
    .subscribe({
      next: response => {
        console.log('Chat API response:', response);

        if (!response?.success || !response.data) {
          this.errorMessage = 'The API returned no answer.';
          this.cdr.detectChanges();
          return;
        }

        this.messages.push({
          role: 'assistant',
          content: response.data.answer,
          sources: response.data.sources ?? []
        });

        this.cdr.detectChanges();
      },
      error: error => {
        console.error('Chat request failed:', error);

        this.errorMessage =
          error.status === 401
            ? 'Your session has expired. Please sign in again.'
            : error.status === 403
              ? 'The request was rejected. Please check your CSRF token.'
              : error.error?.message
                ?? 'Something went wrong while generating the answer.';

        this.cdr.detectChanges();
      }
    });
  }

  onEnter(event: Event): void {
    const keyboardEvent = event as KeyboardEvent;

    if (!keyboardEvent.shiftKey) {
      keyboardEvent.preventDefault();
      this.sendMessage();
    }
  }
}

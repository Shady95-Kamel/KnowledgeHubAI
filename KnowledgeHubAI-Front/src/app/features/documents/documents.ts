import {
  Component,
  OnInit,
  OnDestroy,
  ChangeDetectorRef,
  inject
} from '@angular/core';

import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

import {
  Subject,
  Subscription,
  timer
} from 'rxjs';

import {
  debounceTime,
  distinctUntilChanged,
  exhaustMap
} from 'rxjs/operators';

import type {
  DocumentItem,
  DocumentStatus
} from '../../core/models/doc.model';

import {
  DocumentsService,
  DocumentDto
} from '../../core/services/documents';

@Component({
  selector: 'app-documents',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './documents.html',
  styleUrl: './documents.scss'
})
export class Documents implements OnInit, OnDestroy {
  private readonly documentsService = inject(DocumentsService);
  private readonly cdr = inject(ChangeDetectorRef);

  private readonly searchSubject = new Subject<string>();

  private requestId = 0;

  // Automatic status polling
  private pollingSubscription?: Subscription;
  private readonly pollingInterval = 3000;
  private readonly maxPollingDuration = 2 * 60 * 1000;
  private pollingStartedAt = 0;
  private hasSeenProcessing = false;

  // Upload toast timeout
  private uploadMessageTimeout?: ReturnType<typeof setTimeout>;

  searchTerm = '';
  selectedStatus: DocumentStatus | 'All' = 'All';

  currentPage = 1;
  readonly pageSize = 5;

  documents: DocumentItem[] = [];

  totalCount = 0;
  totalPages = 0;

  isLoading = false;
  errorMessage = '';

  isUploading = false;
  uploadMessage = '';

  // Delete confirmation modal
  showDeleteModal = false;
  documentToDelete: DocumentItem | null = null;
  isDeleting = false;

  // Inline feedback for document actions
  actionMessage = '';
  actionMessageType: 'success' | 'error' | 'info' = 'info';

  ngOnInit(): void {
    this.searchSubject
      .pipe(
        debounceTime(1000),
        distinctUntilChanged()
      )
      .subscribe(() => {
        this.loadDocuments();
      });

    this.loadDocuments();
  }

  ngOnDestroy(): void {
    this.stopStatusPolling();

    this.searchSubject.complete();

    if (this.uploadMessageTimeout) {
      clearTimeout(this.uploadMessageTimeout);
      this.uploadMessageTimeout = undefined;
    }
  }

  // --------------------------------------------------
  // Upload toast
  // --------------------------------------------------

  private showUploadMessage(message: string): void {
    if (this.uploadMessageTimeout) {
      clearTimeout(this.uploadMessageTimeout);
    }

    this.uploadMessage = message;
    this.cdr.detectChanges();

    this.uploadMessageTimeout = setTimeout(() => {
      this.uploadMessage = '';
      this.uploadMessageTimeout = undefined;

      this.cdr.detectChanges();
    }, 3000);
  }

  dismissUploadMessage(): void {
    this.uploadMessage = '';

    if (this.uploadMessageTimeout) {
      clearTimeout(this.uploadMessageTimeout);
      this.uploadMessageTimeout = undefined;
    }

    this.cdr.detectChanges();
  }

  // --------------------------------------------------
  // Document lists
  // --------------------------------------------------

  get filteredDocuments(): DocumentItem[] {
    return this.documents;
  }

  get paginatedDocuments(): DocumentItem[] {
    return this.documents;
  }

  loadDocuments(): void {
    const currentRequestId = ++this.requestId;

    this.isLoading = true;
    this.errorMessage = '';

    this.cdr.detectChanges();

    this.documentsService
      .getMyDocuments(this.getRequestParams())
      .subscribe({
        next: response => {
          // Ignore an older request if a newer one was started.
          if (currentRequestId !== this.requestId) {
            return;
          }

          this.applyDocumentsResponse(response);

          this.isLoading = false;

          // If any visible document is still processing,
          // automatically start polling.
          this.ensureStatusPolling();

          this.cdr.detectChanges();
        },

        error: error => {
          if (currentRequestId !== this.requestId) {
            return;
          }

          console.error('Failed to load documents:', error);

          this.documents = [];
          this.totalCount = 0;
          this.totalPages = 0;
          this.isLoading = false;

          if (error.status === 401) {
            this.errorMessage =
              'Your session has expired. Please sign in again.';
          } else if (error.status === 0) {
            this.errorMessage =
              'Cannot connect to the API. Check that the backend is running and the HTTPS certificate is trusted.';
          } else {
            this.errorMessage =
              'Unable to load documents. Please try again.';
          }

          this.cdr.detectChanges();
        }
      });
  }

  private getRequestParams() {
    return {
      pageNumber: this.currentPage,
      pageSize: this.pageSize,
      searchTerm: this.searchTerm.trim() || undefined,
      status: this.getApiStatus(this.selectedStatus),
      sortBy: 'createdAt',
      descending: true
    };
  }

  private applyDocumentsResponse(response: {
    data: {
      items?: DocumentDto[];
      totalCount: number;
      totalPages: number;
      pageNumber: number;
    };
  }): void {
    const page = response.data;

    this.documents = (page.items ?? [])
      .filter(document =>
        String(document.status).toLowerCase() !== '5' &&
        String(document.status).toLowerCase() !== 'deleted'
      )
      .map(document => this.mapDocument(document));

    this.totalCount = page.totalCount;
    this.totalPages = page.totalPages;
    this.currentPage = page.pageNumber;
  }

  private getApiStatus(
    status: DocumentStatus | 'All'
  ): string | undefined {
    switch (status) {
      case 'Completed':
        return 'Ready';

      case 'Processing':
        return 'Processing';

      case 'Failed':
        return 'Failed';

      case 'All':
      default:
        return undefined;
    }
  }

  private mapDocument(document: DocumentDto): DocumentItem {
    return {
      id: document.id,
      name: document.name || document.originalFileName,
      uploadedAt: document.createdAt,
      size: this.formatFileSize(document.fileSize),
      status: this.mapStatus(document.status)
    };
  }

  private mapStatus(status: number | string): DocumentStatus {
    const normalizedStatus = String(status).toLowerCase();

    switch (normalizedStatus) {
      case '3':
      case 'ready':
      case 'completed':
        return 'Completed';

      case '4':
      case 'failed':
        return 'Failed';

      case '1':
      case '2':
      case 'uploading':
      case 'processing':
      default:
        return 'Processing';
    }
  }

  private formatFileSize(bytes: number): string {
    if (bytes < 1024) {
      return `${bytes} B`;
    }

    if (bytes < 1024 * 1024) {
      return `${(bytes / 1024).toFixed(1)} KB`;
    }

    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
  }

  // --------------------------------------------------
  // Automatic status polling
  // --------------------------------------------------

  private ensureStatusPolling(): void {
    const hasProcessingDocuments = this.documents.some(
      document => document.status === 'Processing'
    );

    if (hasProcessingDocuments) {
      this.hasSeenProcessing = true;
      this.startStatusPolling();
    }
  }

  private startStatusPolling(): void {
    // Don't create multiple polling subscriptions.
    if (this.pollingSubscription && !this.pollingSubscription.closed) {
      return;
    }

    this.pollingStartedAt = Date.now();

    // Remember if a processing document is already visible.
    this.hasSeenProcessing = this.documents.some(
      document => document.status === 'Processing'
    );

    this.pollingSubscription = timer(
      this.pollingInterval,
      this.pollingInterval
    )
      .pipe(
        // Don't start another poll while the previous request is running.
        exhaustMap(() =>
          this.documentsService.getMyDocuments(this.getRequestParams())
        )
      )
      .subscribe({
        next: response => {
          // Don't apply polling results if search/filter/page changed
          // while this request was in flight.
          const pollingRequestId = this.requestId;

          this.applyPollingResponse(response, pollingRequestId);
        },

        error: error => {
          console.error('Failed to refresh document statuses:', error);

          // Keep polling after a temporary error.
          // The next interval will try again.
        }
      });
  }

  private applyPollingResponse(
    response: {
      data: {
        items?: DocumentDto[];
        totalCount: number;
        totalPages: number;
        pageNumber: number;
      };
    },
    requestIdAtStart: number
  ): void {
    /*
     * The request ID can change when the user searches,
     * changes status, or moves to another page.
     *
     * The response is applied only if the current request
     * is still the same.
     */
    if (requestIdAtStart !== this.requestId) {
      return;
    }

    this.applyDocumentsResponse(response);

    const hasProcessingDocuments = this.documents.some(
      document => document.status === 'Processing'
    );

    if (hasProcessingDocuments) {
      this.hasSeenProcessing = true;
    }

    /*
     * Stop when:
     * 1. We previously saw a Processing document, and no visible
     *    documents are processing anymore; or
     * 2. The maximum polling duration has elapsed.
     *
     * If the uploaded document has not appeared in the list yet,
     * polling continues instead of stopping immediately.
     */
    const processingFinished =
      this.hasSeenProcessing && !hasProcessingDocuments;

    const pollingTimedOut =
      Date.now() - this.pollingStartedAt >= this.maxPollingDuration;

    this.cdr.detectChanges();

    if (processingFinished || pollingTimedOut) {
      this.stopStatusPolling();
    }
  }

  private stopStatusPolling(): void {
    this.pollingSubscription?.unsubscribe();
    this.pollingSubscription = undefined;
    this.hasSeenProcessing = false;
    this.pollingStartedAt = 0;
  }

  // --------------------------------------------------
  // Search, filter, pagination
  // --------------------------------------------------

  onSearchChange(): void {
    this.currentPage = 1;
    this.searchSubject.next(this.searchTerm.trim());
  }

  onStatusChange(): void {
    this.currentPage = 1;
    this.loadDocuments();
  }

  goToPage(page: number): void {
    if (
      page < 1 ||
      page > this.totalPages ||
      this.isLoading
    ) {
      return;
    }

    this.currentPage = page;
    this.loadDocuments();
  }

  // --------------------------------------------------
  // Upload document
  // --------------------------------------------------

  onUpload(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];

    if (!file) {
      return;
    }

    this.uploadMessage = '';

    if (!file.name.toLowerCase().endsWith('.pdf')) {
      this.showUploadMessage('Please select a PDF file.');
      input.value = '';
      return;
    }

    const maxFileSize = 20 * 1024 * 1024;

    if (file.size > maxFileSize) {
      this.showUploadMessage('The maximum file size is 20 MB.');
      input.value = '';
      return;
    }

    if (this.isUploading) {
      input.value = '';
      return;
    }

    this.isUploading = true;
    this.cdr.detectChanges();

    this.documentsService.uploadDocument(file).subscribe({
      next: () => {
        this.isUploading = false;

        this.showUploadMessage(
          'Document uploaded successfully.'
        );

        this.currentPage = 1;

        /*
         * Start polling immediately after the upload succeeds.
         * The document may initially appear as Processing.
         */
        this.startStatusPolling();
        this.loadDocuments();

        input.value = '';
        this.cdr.detectChanges();
      },

      error: error => {
        console.error('Failed to upload document:', error);

        this.isUploading = false;

        this.showUploadMessage(
          error.error?.message ??
          'Failed to upload the document.'
        );

        input.value = '';
        this.cdr.detectChanges();
      }
    });
  }

  // --------------------------------------------------
  // Delete document
  // --------------------------------------------------

  onDelete(document: DocumentItem): void {
    if (this.isDeleting) {
      return;
    }

    this.documentToDelete = document;
    this.showDeleteModal = true;
    this.actionMessage = '';
  }

  cancelDelete(): void {
    if (this.isDeleting) {
      return;
    }

    this.showDeleteModal = false;
    this.documentToDelete = null;
  }

  confirmDelete(): void {
    const document = this.documentToDelete;

    if (!document || this.isDeleting) {
      return;
    }

    this.isDeleting = true;
    this.actionMessage = '';

    this.documentsService.deleteDocument(document.id).subscribe({
      next: () => {
        this.isDeleting = false;
        this.showDeleteModal = false;
        this.documentToDelete = null;

        this.actionMessage = 'Document deleted successfully.';
        this.actionMessageType = 'success';

        // If the last item on a page was deleted, go back one page.
        if (this.documents.length === 1 && this.currentPage > 1) {
          this.currentPage--;
        }

        this.loadDocuments();
        this.cdr.detectChanges();
      },

      error: error => {
        console.error('Failed to delete document:', error);

        this.isDeleting = false;
        this.actionMessageType = 'error';

        if (error.status === 401) {
          this.actionMessage =
            'Your session has expired. Please sign in again.';
        } else if (error.status === 404) {
          this.actionMessage =
            'Document not found or it has already been deleted.';
        } else {
          this.actionMessage =
            'Unable to delete the document. Please try again.';
        }

        // Keep the modal open so the user can retry or cancel.
        this.cdr.detectChanges();
      }
    });
  }

  // --------------------------------------------------
  // Retry failed document
  // --------------------------------------------------

  onRetry(document: DocumentItem): void {
    if (this.isLoading) {
      return;
    }

    this.actionMessage = '';

    this.documentsService.retryDocument(document.id).subscribe({
      next: response => {
        console.info(response.message);

        this.actionMessageType = 'info';
        this.actionMessage =
          'The retry request was accepted. The document status will refresh automatically.';

        this.startStatusPolling();
        this.loadDocuments();

        this.cdr.detectChanges();
      },

      error: error => {
        console.error('Failed to retry document:', error);

        this.actionMessageType = 'error';

        if (error.status === 401) {
          this.actionMessage =
            'Your session has expired. Please sign in again.';
        } else if (error.status === 404) {
          this.actionMessage = 'Document not found.';
        } else if (error.status === 409) {
          this.actionMessage =
            'Only failed documents can be retried.';
        } else {
          this.actionMessage =
            'Unable to submit the retry request. Please try again.';
        }

        this.cdr.detectChanges();
      }
    });
  }
}

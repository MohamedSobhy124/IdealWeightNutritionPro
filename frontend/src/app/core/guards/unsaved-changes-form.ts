import { Directive, HostListener, viewChild } from '@angular/core';
import { NgForm } from '@angular/forms';
import { HasUnsavedChanges } from './unsaved-changes.guard';

/**
 * Base for routed template-driven form pages guarded by `unsavedChangesGuard`.
 * Tracks the component's first `<form>`; call `markSaved()` after a successful save.
 */
@Directive()
export abstract class UnsavedChangesForm implements HasUnsavedChanges {
  protected readonly ngForm = viewChild(NgForm);

  hasUnsavedChanges(): boolean {
    return !!this.ngForm()?.dirty;
  }

  protected markSaved(): void {
    this.ngForm()?.form.markAsPristine();
  }

  @HostListener('window:beforeunload', ['$event'])
  protected onBeforeUnload(event: BeforeUnloadEvent): void {
    if (this.hasUnsavedChanges()) event.preventDefault();
  }
}

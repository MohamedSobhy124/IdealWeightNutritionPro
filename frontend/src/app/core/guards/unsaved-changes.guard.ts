import { inject } from '@angular/core';
import { CanDeactivateFn } from '@angular/router';
import { UiConfirmService } from '../../shared/ui/ui-confirm.service';
import { ADMIN_UI } from '../i18n/admin-ui-text';
import { LocaleService } from '../services/locale.service';

/** Implemented by routed components that can hold unsaved edits. */
export interface HasUnsavedChanges {
  hasUnsavedChanges(): boolean;
}

/**
 * Asks for confirmation before leaving a form with unsaved edits.
 * Components should also handle `window:beforeunload` for tab close / reload.
 */
export const unsavedChangesGuard: CanDeactivateFn<HasUnsavedChanges> = (component) => {
  if (!component?.hasUnsavedChanges?.()) return true;
  const locale = inject(LocaleService);
  const t = (key: keyof typeof ADMIN_UI) => locale.pick(ADMIN_UI[key].en, ADMIN_UI[key].ar);
  return inject(UiConfirmService).ask({
    title: t('unsavedChangesTitle'),
    message: t('unsavedChangesMessage'),
    confirmLabel: t('discardChanges'),
    cancelLabel: t('keepEditing'),
    danger: true,
    icon: 'edit_off',
  });
};

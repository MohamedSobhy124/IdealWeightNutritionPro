const STORAGE_KEY = 'iwn.newsletter.email';

export function readGuestNewsletterEmail(): string | null {
  if (typeof sessionStorage === 'undefined') return null;
  const value = sessionStorage.getItem(STORAGE_KEY)?.trim();
  return value || null;
}

export function writeGuestNewsletterEmail(email: string): void {
  if (typeof sessionStorage === 'undefined') return;
  sessionStorage.setItem(STORAGE_KEY, email.trim().toLowerCase());
}

export function clearGuestNewsletterEmail(): void {
  if (typeof sessionStorage === 'undefined') return;
  sessionStorage.removeItem(STORAGE_KEY);
}

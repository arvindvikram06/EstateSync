import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

export const passwordMatchValidator: ValidatorFn = (group: AbstractControl): ValidationErrors | null => {
  const password = group.get('password')?.value as string;
  const confirmPassword = group.get('confirmPassword')?.value as string;

  if (!password || !confirmPassword) return null;

  return password === confirmPassword ? null : { passwordMismatch: true };
};

export function getRoleHome(roles: string[]): string {
  if (roles.includes('Admin'))    return '/admin/dashboard';
  if (roles.includes('Verifier')) return '/verifier/dashboard';
  if (roles.includes('Owner'))    return '/owner/dashboard';
  return '/dashboard';
}

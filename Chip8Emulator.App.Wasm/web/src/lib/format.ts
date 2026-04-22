export function hex4(n: number): string {
  return n.toString(16).padStart(4, '0').toUpperCase();
}

export function hex4(n: number): string {
  return n.toString(16).padStart(4, '0').toUpperCase();
}

export function hex2(n: number): string {
  return n.toString(16).padStart(2, '0').toUpperCase();
}

export function hex3(n: number): string {
  return n.toString(16).padStart(3, '0').toUpperCase();
}

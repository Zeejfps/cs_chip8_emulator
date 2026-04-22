export const viewport = $state({
  width: typeof window === 'undefined' ? 1024 : window.innerWidth,
});

export function initViewport(): () => void {
  const onResize = () => {
    viewport.width = window.innerWidth;
  };
  window.addEventListener('resize', onResize, { passive: true });
  return () => window.removeEventListener('resize', onResize);
}

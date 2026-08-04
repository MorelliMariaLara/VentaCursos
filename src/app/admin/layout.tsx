export default function AdminLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return <div className="admin-page">{children}</div>;
}

export type Role = "student" | "admin";

export interface User {
  id: string;
  name: string;
  email: string;
  passwordHash: string;
  role: Role;
  createdAt: string;
}

export interface Lesson {
  id: string;
  title: string;
  durationMinutes: number;
  /** Remote/local source used only on the server; never exposed to the client. */
  sourceUrl: string;
  order: number;
}

export interface Module {
  id: string;
  title: string;
  lessons: Lesson[];
}

export interface Course {
  id: string;
  slug: string;
  title: string;
  subtitle: string;
  description: string;
  category: string;
  level: "Inicial" | "Intermedio" | "Avanzado";
  price: number;
  currency: "ARS" | "USD";
  durationHours: number;
  includesCertificate: boolean;
  certificateName: string;
  thumbnailGradient: string;
  instructor: string;
  learningOutcomes: string[];
  modules: Module[];
  published: boolean;
  updatedAt?: string;
}

export interface Enrollment {
  id: string;
  userId: string;
  courseId: string;
  purchasedAt: string;
  progress: Record<string, boolean>;
  certificateIssuedAt?: string;
  certificateCode?: string;
  orderId?: string;
}

export type OrderStatus =
  | "pending"
  | "paid"
  | "failed"
  | "rejected"
  | "in_process"
  | "cancelled"
  | "refunded";

export interface Order {
  id: string;
  userId: string;
  courseId: string;
  amount: number;
  currency: string;
  status: OrderStatus;
  createdAt: string;
  updatedAt?: string;
  preferenceId?: string;
  paymentId?: string;
  paymentMethod?: string;
  statusDetail?: string;
  payerEmail?: string;
  simulated?: boolean;
}

export interface DatabaseShape {
  users: User[];
  courses: Course[];
  enrollments: Enrollment[];
  orders: Order[];
}

export interface SessionPayload {
  sub: string;
  email: string;
  name: string;
  role: Role;
}

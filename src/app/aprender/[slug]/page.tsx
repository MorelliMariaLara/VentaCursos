import { notFound, redirect } from "next/navigation";
import { getSession } from "@/lib/auth";
import { getCourseBySlug, getEnrollment, toPublicCourse } from "@/lib/db";
import { LearningClient } from "@/components/LearningClient";

export default async function LearnPage({
  params,
}: {
  params: Promise<{ slug: string }>;
}) {
  const { slug } = await params;
  const session = await getSession();
  if (!session) redirect(`/login?next=/aprender/${slug}`);

  const courseRaw = await getCourseBySlug(slug);
  if (!courseRaw) notFound();

  const enrollment = await getEnrollment(session.sub, courseRaw.id);
  if (!enrollment) redirect(`/cursos/${slug}`);

  const course = toPublicCourse(courseRaw);

  return (
    <LearningClient
      slug={course.slug}
      courseTitle={course.title}
      certificateName={course.certificateName}
      modules={course.modules}
      initialProgress={enrollment.progress}
      certificateCode={enrollment.certificateCode}
      certificateIssuedAt={enrollment.certificateIssuedAt}
    />
  );
}

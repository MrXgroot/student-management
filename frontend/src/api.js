const BASE_URL = '/api/v1';

export async function fetchHealth() {
  try {
    const res = await fetch(`${BASE_URL}/dashboard/health`);
    if (!res.ok) throw new Error('Health check failed');
    return await res.json();
  } catch (err) {
    console.warn('Health check error:', err);
    return { status: 'OFFLINE', isUsingMongoDb: false, persistenceMode: 'Offline/Connecting...' };
  }
}

export async function fetchDashboard() {
  const res = await fetch(`${BASE_URL}/dashboard`);
  if (!res.ok) throw new Error(`Failed to load dashboard: ${res.statusText}`);
  return await res.json();
}

export async function fetchStudents(search = '', status = 'ALL', program = 'ALL') {
  const params = new URLSearchParams();
  if (search) params.append('search', search);
  if (status && status !== 'ALL') params.append('status', status);
  if (program && program !== 'ALL') params.append('program', program);

  const res = await fetch(`${BASE_URL}/students?${params.toString()}`);
  if (!res.ok) throw new Error(`Failed to fetch students: ${res.statusText}`);
  return await res.json();
}

export async function fetchStudentById(id) {
  const res = await fetch(`${BASE_URL}/students/${id}`);
  if (!res.ok) throw new Error(`Failed to fetch student details: ${res.statusText}`);
  return await res.json();
}

export async function createStudent(studentData) {
  const res = await fetch(`${BASE_URL}/students`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(studentData),
  });
  if (!res.ok) {
    const errorData = await res.json().catch(() => ({}));
    const message = errorData.title || errorData.message || 'Failed to create student';
    throw new Error(message);
  }
  return await res.json();
}

export async function updateStudent(id, studentData) {
  const res = await fetch(`${BASE_URL}/students/${id}`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(studentData),
  });
  if (!res.ok) {
    const errorData = await res.json().catch(() => ({}));
    const message = errorData.title || errorData.message || 'Failed to update student';
    throw new Error(message);
  }
  return await res.json();
}

export async function deleteStudent(id) {
  const res = await fetch(`${BASE_URL}/students/${id}`, {
    method: 'DELETE',
  });
  if (!res.ok) throw new Error('Failed to delete student');
  return true;
}

export async function resetDemoData() {
  const res = await fetch(`${BASE_URL}/students/seed-reset`, {
    method: 'POST',
  });
  if (!res.ok) throw new Error('Failed to reset demo data');
  return await res.json();
}

// Academic & Course Catalog APIs
export async function fetchPrograms() {
  const res = await fetch(`${BASE_URL}/programs`);
  if (!res.ok) throw new Error(`Failed to fetch programs: ${res.statusText}`);
  return await res.json();
}

export async function fetchProgramDetails(code) {
  const res = await fetch(`${BASE_URL}/programs/${encodeURIComponent(code)}`);
  if (!res.ok) throw new Error(`Failed to fetch program details: ${res.statusText}`);
  return await res.json();
}

export async function fetchAcademicPeriods() {
  const res = await fetch(`${BASE_URL}/academic/periods`);
  if (!res.ok) throw new Error(`Failed to fetch academic periods: ${res.statusText}`);
  return await res.json();
}

export async function fetchCurrentAcademicPeriod() {
  const res = await fetch(`${BASE_URL}/academic/periods/current`);
  if (!res.ok) throw new Error(`Failed to fetch current academic period: ${res.statusText}`);
  return await res.json();
}

export async function fetchCourses(search = '', program = 'ALL', semester = 0, status = 'ALL') {
  const params = new URLSearchParams();
  if (search) params.append('search', search);
  if (program && program !== 'ALL') params.append('program', program);
  if (semester && semester > 0) params.append('semester', semester);
  if (status && status !== 'ALL') params.append('status', status);

  const res = await fetch(`${BASE_URL}/courses?${params.toString()}`);
  if (!res.ok) throw new Error(`Failed to fetch courses: ${res.statusText}`);
  return await res.json();
}

export async function fetchCourseById(id) {
  const res = await fetch(`${BASE_URL}/courses/${id}`);
  if (!res.ok) throw new Error(`Failed to fetch course details: ${res.statusText}`);
  return await res.json();
}

export async function createCourse(courseData) {
  const res = await fetch(`${BASE_URL}/courses`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(courseData),
  });
  if (!res.ok) {
    const errorData = await res.json().catch(() => ({}));
    const message = errorData.message || errorData.title || 'Failed to create course';
    throw new Error(message);
  }
  return await res.json();
}

export async function updateCourse(id, courseData) {
  const res = await fetch(`${BASE_URL}/courses/${id}`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(courseData),
  });
  if (!res.ok) {
    const errorData = await res.json().catch(() => ({}));
    const message = errorData.message || errorData.title || 'Failed to update course';
    throw new Error(message);
  }
  return await res.json();
}

export async function deleteCourse(id) {
  const res = await fetch(`${BASE_URL}/courses/${id}`, {
    method: 'DELETE',
  });
  if (!res.ok) throw new Error('Failed to delete course');
  return true;
}

export async function resetAcademicSeeds() {
  const res = await fetch(`${BASE_URL}/courses/seed-reset`, {
    method: 'POST',
  });
  if (!res.ok) throw new Error('Failed to reset academic demo data');
  return await res.json();
}

// Term Registration & Course Enrollment APIs
export async function fetchRegistrations(period = '', program = '', semester = 0, status = '', search = '') {
  const params = new URLSearchParams();
  if (period && period !== 'ALL') params.append('period', period);
  if (program && program !== 'ALL') params.append('program', program);
  if (semester && semester > 0) params.append('semester', semester);
  if (status && status !== 'ALL') params.append('status', status);
  if (search) params.append('search', search);

  const res = await fetch(`${BASE_URL}/registrations?${params.toString()}`);
  if (!res.ok) throw new Error(`Failed to fetch term registrations: ${res.statusText}`);
  return await res.json();
}

export async function fetchRegistrationById(id) {
  const res = await fetch(`${BASE_URL}/registrations/${id}`);
  if (!res.ok) throw new Error(`Failed to fetch registration: ${res.statusText}`);
  return await res.json();
}

export async function fetchRegistrationsByStudentId(studentId) {
  const res = await fetch(`${BASE_URL}/registrations/student/${studentId}`);
  if (!res.ok) throw new Error(`Failed to fetch student registrations: ${res.statusText}`);
  return await res.json();
}

export async function fetchRegistrationOverview(period = '2026-ODD', program = '', semester = 0, search = '') {
  const params = new URLSearchParams();
  if (period) params.append('period', period);
  if (program && program !== 'ALL') params.append('program', program);
  if (semester && semester > 0) params.append('semester', semester);
  if (search) params.append('search', search);

  const res = await fetch(`${BASE_URL}/registrations/overview?${params.toString()}`);
  if (!res.ok) throw new Error(`Failed to fetch registration overview: ${res.statusText}`);
  return await res.json();
}

export async function registerStudentForTerm(data) {
  const res = await fetch(`${BASE_URL}/registrations`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(data),
  });
  if (!res.ok) {
    const errorData = await res.json().catch(() => ({}));
    const message = errorData.message || errorData.title || 'Failed to register student for term';
    throw new Error(message);
  }
  return await res.json();
}

export async function updateRegistrationStatus(id, status) {
  const res = await fetch(`${BASE_URL}/registrations/${id}/status`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ status }),
  });
  if (!res.ok) {
    const errorData = await res.json().catch(() => ({}));
    const message = errorData.message || errorData.title || 'Failed to update registration status';
    throw new Error(message);
  }
  return await res.json();
}

export async function fetchEnrolledCourses(registrationId) {
  const res = await fetch(`${BASE_URL}/registrations/${registrationId}/courses`);
  if (!res.ok) throw new Error(`Failed to fetch enrolled courses: ${res.statusText}`);
  return await res.json();
}

export async function fetchEligibleCourses(registrationId) {
  const res = await fetch(`${BASE_URL}/registrations/${registrationId}/eligible-courses`);
  if (!res.ok) throw new Error(`Failed to fetch eligible courses: ${res.statusText}`);
  return await res.json();
}

export async function enrollCourseInRegistration(registrationId, courseId) {
  const res = await fetch(`${BASE_URL}/registrations/${registrationId}/courses`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ courseId }),
  });
  if (!res.ok) {
    const errorData = await res.json().catch(() => ({}));
    const message = errorData.message || errorData.title || 'Failed to enroll in course';
    throw new Error(message);
  }
  return await res.json();
}

export async function dropCourseFromRegistration(registrationId, courseId) {
  const res = await fetch(`${BASE_URL}/registrations/${registrationId}/courses/${courseId}`, {
    method: 'DELETE',
  });
  if (!res.ok) {
    const errorData = await res.json().catch(() => ({}));
    const message = errorData.message || errorData.title || 'Failed to drop course';
    throw new Error(message);
  }
  return true;
}

// Attendance APIs
export async function fetchAttendanceSheet(courseId, date = '', period = '2026-ODD') {
  const params = new URLSearchParams();
  if (courseId) params.append('courseId', courseId);
  if (date) params.append('date', date);
  if (period) params.append('period', period);

  const res = await fetch(`${BASE_URL}/attendance?${params.toString()}`);
  if (!res.ok) {
    const errorData = await res.json().catch(() => ({}));
    const message = errorData.message || errorData.title || 'Failed to load attendance sheet';
    throw new Error(message);
  }
  return await res.json();
}

export async function saveAttendanceBatch(batchData) {
  const res = await fetch(`${BASE_URL}/attendance`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(batchData),
  });
  if (!res.ok) {
    const errorData = await res.json().catch(() => ({}));
    const message = errorData.message || errorData.title || 'Failed to save attendance';
    throw new Error(message);
  }
  return await res.json();
}

export async function fetchAttendanceHistory(courseId, period = '2026-ODD') {
  const params = new URLSearchParams();
  if (courseId) params.append('courseId', courseId);
  if (period) params.append('period', period);

  const res = await fetch(`${BASE_URL}/attendance/history?${params.toString()}`);
  if (!res.ok) {
    const errorData = await res.json().catch(() => ({}));
    const message = errorData.message || errorData.title || 'Failed to load attendance history';
    throw new Error(message);
  }
  return await res.json();
}

export async function fetchStudentAttendance(studentId, courseId = '', period = '2026-ODD') {
  const params = new URLSearchParams();
  if (courseId) params.append('courseId', courseId);
  if (period) params.append('period', period);

  const res = await fetch(`${BASE_URL}/attendance/student/${encodeURIComponent(studentId)}?${params.toString()}`);
  if (!res.ok) throw new Error(`Failed to load student attendance: ${res.statusText}`);
  return await res.json();
}

// Fee Management APIs
export async function fetchFeeOverview(period = '2026-ODD', program = '', status = '', search = '') {
  const params = new URLSearchParams();
  if (period) params.append('period', period);
  if (program && program !== 'ALL') params.append('program', program);
  if (status && status !== 'ALL') params.append('status', status);
  if (search) params.append('search', search);

  const res = await fetch(`${BASE_URL}/fees/overview?${params.toString()}`);
  if (!res.ok) {
    const errorData = await res.json().catch(() => ({}));
    const message = errorData.message || errorData.title || 'Failed to load fee overview';
    throw new Error(message);
  }
  return await res.json();
}

export async function fetchProgramFeeStructures(period = '2026-ODD') {
  const params = new URLSearchParams();
  if (period) params.append('period', period);

  const res = await fetch(`${BASE_URL}/fees/structures?${params.toString()}`);
  if (!res.ok) throw new Error(`Failed to load fee structures: ${res.statusText}`);
  return await res.json();
}

export async function updateProgramFeeStructure(programCode, tuitionFeePaise, otherFeesPaise, period = '2026-ODD') {
  const params = new URLSearchParams();
  if (period) params.append('period', period);

  const res = await fetch(`${BASE_URL}/fees/structures/${encodeURIComponent(programCode)}?${params.toString()}`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ tuitionFeePaise, otherFeesPaise })
  });
  if (!res.ok) {
    const errorData = await res.json().catch(() => ({}));
    const message = errorData.message || errorData.title || 'Failed to update fee structure';
    throw new Error(message);
  }
  return await res.json();
}

export async function fetchStudentFeeDetail(studentId, period = '2026-ODD') {
  const params = new URLSearchParams();
  if (period) params.append('period', period);

  const res = await fetch(`${BASE_URL}/fees/students/${encodeURIComponent(studentId)}?${params.toString()}`);
  if (!res.ok) {
    const errorData = await res.json().catch(() => ({}));
    const message = errorData.message || errorData.title || 'Failed to load student fee details';
    throw new Error(message);
  }
  return await res.json();
}

export async function recordFeePayment(paymentData) {
  const res = await fetch(`${BASE_URL}/fees/payments`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(paymentData)
  });
  if (!res.ok) {
    const errorData = await res.json().catch(() => ({}));
    const message = errorData.message || errorData.title || 'Failed to record fee payment';
    throw new Error(message);
  }
  return await res.json();
}

export async function fetchPaymentHistory(studentId = '', period = '', limit = 50) {
  const params = new URLSearchParams();
  if (studentId) params.append('studentId', studentId);
  if (period && period !== 'ALL') params.append('period', period);
  if (limit) params.append('limit', limit);

  const res = await fetch(`${BASE_URL}/fees/payments?${params.toString()}`);
  if (!res.ok) throw new Error(`Failed to load payment history: ${res.statusText}`);
  return await res.json();
}





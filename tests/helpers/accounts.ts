export const accounts = {
  admin:        { email: 'admin@pertamina.com',          password: '123456', role: 'Admin' },
  hc:           { email: 'meylisa.tjiang@pertamina.com', password: '123456', role: 'HC' },
  coachee:      { email: 'rino.prasetyo@pertamina.com',  password: '123456', role: 'Coachee' },
  coachee2:     { email: 'iwan3@pertamina.com',          password: '123456', role: 'Coachee' },
  direktur:     { email: 'direktur@pertamina.com',       password: '123456', role: 'Direktur' },
  vp:           { email: 'vp@pertamina.com',             password: '123456', role: 'VP' },
  manager:      { email: 'manager@pertamina.com',        password: '123456', role: 'Manager' },
  sectionHead:  { email: 'taufik.hartopo@pertamina.com', password: '123456', role: 'SectionHead' },
  srSupervisor: { email: 'choirul.anam@pertamina.com',   password: '123456', role: 'SrSupervisor' },
  coach:        { email: 'rustam.nugroho@pertamina.com', password: '123456', role: 'Coach' },
} as const;

export type AccountKey = keyof typeof accounts;

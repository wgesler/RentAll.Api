export interface EmailRequest {
  organizationId: string;
  officeId: number;
  fromEmail: string;
  fromName: string;
  toEmail: string;
  toName: string;
  subject: string;
  plainTextContent: string;
  htmlContent: string;
}

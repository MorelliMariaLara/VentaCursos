import { NextResponse } from "next/server";
import {
  allowSimulatePayments,
  getPublicKey,
  isMercadoPagoConfigured,
} from "@/lib/mercadopago";

export async function GET() {
  return NextResponse.json({
    configured: isMercadoPagoConfigured(),
    simulate: allowSimulatePayments(),
    publicKey: getPublicKey(),
  });
}

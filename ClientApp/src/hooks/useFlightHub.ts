import { useEffect, useRef, useState } from 'react';
import * as signalR from '@microsoft/signalr';
import type { FlightStatusChangedEvent, FlightPositionUpdatedEvent } from '../api/types';

interface ScheduleSyncedEvent {
  created: number;
  updated: number;
  timestampUtc: string;
}

interface FlightHubCallbacks {
  onStatusChanged?: (evt: FlightStatusChangedEvent) => void;
  onPositionUpdated?: (evt: FlightPositionUpdatedEvent) => void;
  onDeparted?: (evt: FlightStatusChangedEvent) => void;
  onLanded?: (evt: FlightStatusChangedEvent) => void;
  /** Canlı tarife kaynağı (AeroDataBox/AviationStack) bir uçuşu İPTAL EDİLDİ olarak işaretlediğinde tetiklenir. */
  onCancelled?: (evt: FlightStatusChangedEvent) => void;
  /** Arka planda yeni uçuş(lar) senkronize edildiğinde tetiklenir; liste yeniden çekilebilir. */
  onScheduleSynced?: (evt: ScheduleSyncedEvent) => void;
}

/**
 * /hubs/flights SignalR hub'ına bağlanır. Kullanıcı sayfayı yenilemeden canlı durum ve
 * konum güncellemelerini alır (şartname bölüm 17). Bağlantı koparsa otomatik yeniden dener.
 */
export function useFlightHub(callbacks: FlightHubCallbacks) {
  const [isConnected, setIsConnected] = useState(false);
  const callbacksRef = useRef(callbacks);
  callbacksRef.current = callbacks;

  useEffect(() => {
    const connection = new signalR.HubConnectionBuilder()
      .withUrl('/hubs/flights')
      .withAutomaticReconnect([0, 2000, 5000, 10000, 15000])
      .configureLogging(signalR.LogLevel.Warning)
      .build();

    connection.on('FlightStatusChanged', (evt: FlightStatusChangedEvent) => callbacksRef.current.onStatusChanged?.(evt));
    connection.on('FlightPositionUpdated', (evt: FlightPositionUpdatedEvent) => callbacksRef.current.onPositionUpdated?.(evt));
    connection.on('FlightDeparted', (evt: FlightStatusChangedEvent) => callbacksRef.current.onDeparted?.(evt));
    connection.on('FlightLanded', (evt: FlightStatusChangedEvent) => callbacksRef.current.onLanded?.(evt));
    connection.on('FlightCancelled', (evt: FlightStatusChangedEvent) => callbacksRef.current.onCancelled?.(evt));
    connection.on('ScheduleSynced', (evt: ScheduleSyncedEvent) => callbacksRef.current.onScheduleSynced?.(evt));

    connection.onreconnected(() => setIsConnected(true));
    connection.onreconnecting(() => setIsConnected(false));
    connection.onclose(() => setIsConnected(false));

    connection
      .start()
      .then(() => setIsConnected(true))
      .catch((err) => console.warn('SignalR bağlantısı kurulamadı (canlı güncellemeler devre dışı):', err));

    return () => {
      connection.stop();
    };
  }, []);

  return { isConnected };
}

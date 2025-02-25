import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { MessageStats } from '../models/message-stats.model';

@Injectable({
  providedIn: 'root'
})
export class MessageService {
  private apiUrl = 'http://localhost:5000/api/message'; // Adjust port as needed

  constructor(private http: HttpClient) {}

  getMessagesByAccount(account?: string): Observable<Record<string, MessageStats>> {
    const url = account 
      ? `${this.apiUrl}/messages/by-account?account=${account}`
      : `${this.apiUrl}/messages/by-account`;
    return this.http.get<Record<string, MessageStats>>(url);
  }

  getMessagesByPhoneNumber(phoneNumber?: string): Observable<Record<string, MessageStats>> {
    const url = phoneNumber
      ? `${this.apiUrl}/messages/by-phone?phoneNumber=${phoneNumber}`
      : `${this.apiUrl}/messages/by-phone`;
    return this.http.get<Record<string, MessageStats>>(url);
  }
} 
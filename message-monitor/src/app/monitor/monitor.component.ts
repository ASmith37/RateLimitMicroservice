import { Component, OnInit, OnDestroy } from '@angular/core';
import { interval, Subscription } from 'rxjs';
import { MessageService } from '../services/message.service';
import { MessageStats } from '../models/message-stats.model';

@Component({
  selector: 'app-monitor',
  templateUrl: './monitor.component.html',
  styleUrls: ['./monitor.component.css']
})

export class MonitorComponent implements OnInit, OnDestroy {
  accountFilter: string = '';
  numberFilter: string = '';
  accountStats: Map<string, MessageStats> = new Map();
  numberStats: Map<string, MessageStats> = new Map();
  private updateSubscription?: Subscription;

  // Initialize stats with a default value
  stats: MessageStats | null = null; // or use an empty object if preferred

  constructor(private messageService: MessageService) {}

  ngOnInit() {
    this.updateSubscription = interval(1000).subscribe(() => {
      this.updateStats();
    });
  }

  ngOnDestroy() {
    if (this.updateSubscription) {
      this.updateSubscription.unsubscribe();
    }
  }

  private updateStats() {
    this.messageService.getMessagesByAccount(this.accountFilter)
      .subscribe(stats => {
        this.accountStats = new Map(Object.entries(stats));
        // Ensure stats are updated correctly
      });
    
    this.messageService.getMessagesByPhoneNumber(this.numberFilter)
      .subscribe(stats => {
        this.numberStats = new Map(Object.entries(stats));
        // Ensure stats are updated correctly
      });
  }
} 
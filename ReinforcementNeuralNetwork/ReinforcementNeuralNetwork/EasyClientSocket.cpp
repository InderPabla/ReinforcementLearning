#include "stdafx.h"
#include <iostream>

#define _WINSOCK_DEPRECATED_NO_WARNINGS
#include<stdio.h>
#include<winsock2.h>

#pragma comment(lib,"ws2_32.lib") //Winsock Library

using namespace std;

class EasyClientSocket {

	public:
		EasyClientSocket(int port, const char *address) {
			char *sendbuf = "this is a test";
			WSADATA wsa;
			SOCKET s;
			struct sockaddr_in server;

			printf("\nInitialising Winsock...");
			if (WSAStartup(MAKEWORD(2, 2), &wsa) != 0)
			{
				printf("Failed. Error Code : %d", WSAGetLastError());
			}

			printf("Initialised.\n");

			//Create a socket
			if ((s = socket(AF_INET, SOCK_STREAM, 0)) == INVALID_SOCKET)
			{
				printf("Could not create socket : %d", WSAGetLastError());
			}

			printf("Socket created.\n");


			server.sin_addr.s_addr = inet_addr(address);
			server.sin_family = AF_INET;
			server.sin_port = htons(port);

			//Connect to remote server
			if (connect(s, (struct sockaddr *)&server, sizeof(server)) < 0)
			{
				puts("connect error");
			}

			string outMessage = "tesqererq rrqer";
			//int iResult = send(s, sendbuf, (int)strlen(sendbuf), 0);
			int iResult = send(s, outMessage.c_str(), outMessage.size(), 0);
			if (iResult == SOCKET_ERROR) {
				printf("send failed with error: %d\n", WSAGetLastError());
				
				WSACleanup();
			}

			printf("Bytes Sent: %ld\n", iResult);
			closesocket(s);
			puts("Connected");
		}
};
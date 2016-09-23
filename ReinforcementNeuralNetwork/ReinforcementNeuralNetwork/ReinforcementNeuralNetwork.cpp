// ReinforcementNeuralNetwork.cpp : Defines the entry point for the console application.
//

#include "stdafx.h"
#include<iostream>    //cout
#include<stdio.h> //printf
#include<string>  //string
#include<string.h>    //strlen
#include<cmath> //math lib

#include "NetMaker.cpp"
#include "NetLoader.cpp"
#include "EasyClientSocket.cpp"
using namespace std;


int main()
{
	
	bool create_net = true;
	bool train_net = true;

	const char *address = "127.0.0.1";
	int port = 12345;
	bool debug_mode = false;
	int epoch_save = 100;
	int layer_size = 4;
	float learning_rate = 0.001f; //0.0001f works with sig out
	unsigned int layer_details[] = { 1,32,32, 2 };
	const char *netname = "seeker.net";
	/*const char *dataname = "xor.data";*/

	NetMaker *maker;
	NetLoader *loader;
	EasyClientSocket *client;

	//ACTION AND STATE SIZE ARE IMPORTANT!!
	int stateSize = 1;
	int actionSize = 2;

	if (create_net == true) {
		maker = new NetMaker(layer_details, layer_size, netname);
		delete maker;
	}

	loader = new NetLoader(netname, epoch_save, learning_rate);
	client = new EasyClientSocket(port, address, debug_mode);
	client->OpenConnection(); //open connection
	
	//--------Connection conversation start--------
	while (true) {
		try {

			//1. Get current state, make prediction and send action 
			float *rcvStateArray = client->ReceiveFloatArray(stateSize);
			fann_type *input = new fann_type[stateSize];
			for (int i = 0; i < stateSize; i++) {
				input[i] = rcvStateArray[i];
			}
			fann_type *prediction = loader->predict(input);
			float *sendActionArray = new float[actionSize];
			for (int i = 0; i < actionSize; i++) {
				sendActionArray[i] = prediction[i];
			}
			client->SendArray(sendActionArray, actionSize);

			//2. Get new state, make new prediction
			float *rcvStateArray2 = client->ReceiveFloatArray(stateSize);
			fann_type *input2 = new fann_type[stateSize];
			for (int i = 0; i < stateSize; i++) {
				input2[i] = rcvStateArray2[i];
			}
			fann_type *prediction2 = loader->predict(input2);
			float *sendActionArray2 = new float[actionSize];
			for (int i = 0; i < actionSize; i++) {
				sendActionArray2[i] = prediction2[i];
			}
			client->SendArray(sendActionArray2, actionSize);

			//3. Get target output and train with input 2
			float *rcvActionArray = client->ReceiveFloatArray(actionSize);
			fann_type *expectedOutput = new fann_type[actionSize];
			for (int i = 0; i < actionSize; i++) {
				expectedOutput[i] = rcvActionArray[i];
			}

			if (train_net == true) {
				
				loader->fit(input2, expectedOutput);
				loader->mean_squared_error();
				//float mse = pow((input2[0] - expectedOutput[0]), 2.0f);
				//printf("%f %f %f\n", input2[0], expectedOutput[0], mse);
			}

			delete[] rcvActionArray;
			delete[] expectedOutput;
			delete[] sendActionArray;
			delete[] rcvStateArray;
			delete[] input;
			delete[] sendActionArray2;
			delete[] rcvStateArray2;
			delete[] input2;
		}
		catch (int e) {
			printf("EXCEPTION %i",e);
			break;
		}
	}

	//--------Connection conversation end--------

	client->CloseConnection(); //close connection

	//free heap
	delete client;

	return 0;
}


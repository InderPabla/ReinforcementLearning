// ReinforcementNeuralNetwork.cpp : Defines the entry point for the console application.
//

#include "stdafx.h"
#include <iostream>
#include <stdio.h>
#include "floatfann.h"
#include "fann_cpp.h"

#include <ios>
#include <iomanip>
#include <string>

int main()
{
	std::cout << "Hello World! ";
	std::cout << "I'm a C++ program";

	//TRAIN

	const unsigned int num_input = 2;
	const unsigned int num_output = 1;
	const unsigned int num_layers = 3;
	const unsigned int num_neurons_hidden = 3;
	const float desired_error = (const float) 0.000001;
	const unsigned int max_epochs = 500000;
	const unsigned int epochs_between_reports = 1000;
	unsigned int i;
	int ret = 0;

	fann_type *calc_out;
	struct fann_train_data *data;
	struct fann *ann = fann_create_standard(num_layers, num_input, num_neurons_hidden, num_output);

	fann_set_activation_function_hidden(ann, FANN_SIGMOID_SYMMETRIC);
	fann_set_activation_function_output(ann, FANN_SIGMOID_SYMMETRIC);

	fann_train_on_file(ann, "xor.data", max_epochs, epochs_between_reports, desired_error);

	fann_save(ann, "xor_float.net");

	fann_destroy(ann);
	
	//TEST

	printf("Creating network.\n");
	ann = fann_create_from_file("xor_float.net");

	if (!ann)
	{
		printf("Error creating ann --- ABORTING.\n");
		return -1;
	}

	fann_print_connections(ann);
	fann_print_parameters(ann);

	printf("Testing network.\n");

	data = fann_read_train_from_file("xor.data");

	for (i = 0; i < fann_length_train_data(data); i++)
	{
		fann_reset_MSE(ann);
		calc_out = fann_test(ann, data->input[i], data->output[i]);

		printf("XOR test (%f, %f) -> %f, should be %f, difference=%f\n",
			data->input[i][0], data->input[i][1], calc_out[0], data->output[i][0],
			(float)fann_abs(calc_out[0] - data->output[i][0]));
	}

	printf("Cleaning up.\n");
	fann_destroy_train(data);
	fann_destroy(ann);
	

	while (true) {}
	return 0;

	

}


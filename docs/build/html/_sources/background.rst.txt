
DynamoStudentManager Integrations
=================================

This example project was built to test local DynamoDB docker container.  I then added
the following libraries:

.. hlist::
   :columns: 1

   * **Serilog** with logging sinks for Seq and Elasticsearch
   * **Prometheus-net** with simple statuscode metrics
   * **Nuke** build for building and packaging application
   * **Swashbuckle** for Swagger interface: /swagger


Other tools/integrations added:

.. hlist::
   :columns: 1
   
   * **Sphinx** documentation with OpenAPI specs and these extensions:
       #. sphinx.ext.autodoc
       #. sphinxcontrib.openapi
   * Integrated Health endpoint: /health
